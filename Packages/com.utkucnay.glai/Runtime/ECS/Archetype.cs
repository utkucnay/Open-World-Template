using System;
using System.Text;
using Glai.Allocator;
using Glai.Collection;
using Glai.Collections;
using Glai.Core;

namespace Glai.ECS.Core
{
    public partial struct Archetype : IEquatable<Archetype>
    {
        const int ChunkCapacityBytes = 32 * 1024;
        const int MaxChunkCount = 500;

        private FixedList<Chunk> chunkList;

        private FixedArray<bool> chunkIsAvailable;
        private FixedStack<int> availableChunkIndices;

        private FixedArray<int> componentTypeIds;
        private FixedArray<int> componentSizes;
        private FixedArray<int> componentStorageMap; // maps type-id position → chunk storage index, or -1 for zero-size tags
        private int componentCount;
        private int dataComponentCount;
        private MemoryStateHandle memoryStateHandle;

        internal Archetype(in ArchetypeData archetypeData, MemoryStateHandle memoryStateHandle, MemoryState memoryState)
        {
            this.memoryStateHandle = memoryStateHandle;
            componentCount = archetypeData.ComponentCount;

            chunkList = new FixedList<Chunk>(MaxChunkCount, memoryStateHandle, memoryState);
            componentTypeIds = new FixedArray<int>(componentCount, archetypeData.ComponentTypeIds, memoryStateHandle, memoryState);
            componentSizes = new FixedArray<int>(componentCount, archetypeData.ComponentSizes, memoryStateHandle, memoryState);
            componentStorageMap = new FixedArray<int>(componentCount, memoryStateHandle, memoryState);
            chunkIsAvailable = new FixedArray<bool>(MaxChunkCount, memoryStateHandle, memoryState);
            availableChunkIndices = new FixedStack<int>(MaxChunkCount, memoryStateHandle, memoryState);

            for (int i = 0; i < MaxChunkCount; i++)
                chunkIsAvailable[i] = false;

            for (int i = 1; i < componentCount; i++)
            {
                int keyId   = componentTypeIds[i];
                int keySize = componentSizes[i];
                int j = i - 1;
                while (j >= 0 && componentTypeIds[j] > keyId)
                {
                    componentTypeIds[j + 1] = componentTypeIds[j];
                    componentSizes[j + 1]   = componentSizes[j];
                    j--;
                }
                componentTypeIds[j + 1] = keyId;
                componentSizes[j + 1]   = keySize;
            }

            int chunkIdx = 0;
            for (int i = 0; i < componentCount; i++)
            {
                componentStorageMap[i] = componentSizes[i] > 0 ? chunkIdx++ : -1;
            }
            dataComponentCount = chunkIdx;

            chunkList.Add(CreateChunk(0, memoryState));
            chunkIsAvailable[0] = true;
            availableChunkIndices.Push(0);
        }

        private Chunk CreateChunk(int index, MemoryState memoryState)
        {
            Span<int> dataSizes = stackalloc int[dataComponentCount];
            int j = 0;
            for (int i = 0; i < componentCount; i++)
            {
                if (componentSizes[i] > 0)
                    dataSizes[j++] = componentSizes[i];
            }

            return new Chunk(new ChunkData
            {
                name = new FixedString128Bytes($"Chunk{index}"),
                capacityBytes = ChunkCapacityBytes,
                componentCount = dataComponentCount,
                componentSizes = dataSizes
            }, memoryStateHandle, IEntityManager.Instance.ECSMemoryState.PopChunkStackHandle(), memoryState);
        }

        public void Dispose(MemoryState memoryState)
        {
            for (int i = 0; i < chunkList.Count; i++)
            {
                chunkList[i].Dispose(memoryState);
            }

            chunkList.Dispose(memoryState);
            componentTypeIds.Dispose(memoryState);
            componentSizes.Dispose(memoryState);
            componentStorageMap.Dispose(memoryState);
            chunkIsAvailable.Dispose(memoryState);
            availableChunkIndices.Dispose(memoryState);
        }

        public EntityRecord AddEntity(MemoryState memoryState, int entityId)
        {
            if (availableChunkIndices.Count == 0)
            {
                int newChunkIndex = chunkList.Count;
                chunkList.Add(CreateChunk(newChunkIndex, memoryState));
                chunkIsAvailable[newChunkIndex] = true;
                availableChunkIndices.Push(newChunkIndex);
            }

            int chunkIndex = availableChunkIndices.Pop();
            chunkIsAvailable[chunkIndex] = false;
            ref var chunk = ref chunkList.Get(chunkIndex);
            int componentIndex = chunk.CreateSlot(entityId);

            if (!chunk.IsFull())
            {
                chunkIsAvailable[chunkIndex] = true;
                availableChunkIndices.Push(chunkIndex);
            }

            return new EntityRecord()
            {
                ChunkIndex = chunkIndex,
                ComponentIndex = componentIndex
            };
        }

        public int RemoveEntity(EntityRecord entityRecord)
        {
            int idx = entityRecord.ChunkIndex;
            bool wasFull = !chunkIsAvailable[idx];
            ref var chunk = ref chunkList.Get(idx);
            int swappedEntityId = chunk.RemoveSlot(entityRecord.ComponentIndex);

            if (wasFull)
            {
                chunkIsAvailable[idx] = true;
                availableChunkIndices.Push(idx);
            }

            return swappedEntityId;
        }

        public Buffer<T> GetBuffer<T>(EntityRecord entityRecord) where T : unmanaged, IBufferComponent
        {
            int componentIndex = GetComponentStorageIndex(TypeId<T>.Id);
            if (componentIndex == -1)
                throw new InvalidOperationException($"Buffer component {typeof(T)} not found in archetype.");

            var (slotPtr, capacity) = chunkList[entityRecord.ChunkIndex].GetBuffer<T>(componentIndex, entityRecord.ComponentIndex);
            return new Buffer<T>(slotPtr, capacity);
        }

        public ref T GetComponent<T>(EntityRecord entityRecord) where T : unmanaged, IComponent
        {
            int componentIndex = GetComponentStorageIndex(TypeId<T>.Id);
            if (componentIndex == -1)
            {
                throw new InvalidOperationException($"Component of type {typeof(T)} not found in archetype.");
            }

            return ref chunkList[entityRecord.ChunkIndex].GetComponent<T>(componentIndex, entityRecord.ComponentIndex);
        }

        public bool HasComponent<T>() where T : unmanaged, IComponent
        {
            return HasComponentTypeId(TypeId<T>.Id);
        }

        public bool HasComponentTypeId(int componentTypeId)
        {
            for (int i = 0; i < componentCount; i++)
            {
                if (componentTypeIds[i] == componentTypeId)
                    return true;
            }
            return false;
        }

        /*
         * @template ArchetypeHasAll
         * hintName: Archetype.HasAll.Template.g.cs
         * namespace: Glai.ECS.Core
         * container: public partial struct Archetype
         * maxArity: 7
         * slot: T
         * constraint: unmanaged, IComponent
         * public bool HasAll<{{GENERICS}}>()
         * {{WHERE_BLOCK}}
         * {
         *     return {{#join T " && "}}HasComponent<{{TYPE_NAME}}>(){{/join}};
         * }
         */

        /*
         * @template ArchetypeHasAny
         * hintName: Archetype.HasAny.Template.g.cs
         * namespace: Glai.ECS.Core
         * container: public partial struct Archetype
         * maxArity: 7
         * slot: T
         * constraint: unmanaged, IComponent
         * public bool HasAny<{{GENERICS}}>()
         * {{WHERE_BLOCK}}
         * {
         *     return {{#join T " || "}}HasComponent<{{TYPE_NAME}}>(){{/join}};
         * }
         */

        /*
         * @template ArchetypeHasNone
         * hintName: Archetype.HasNone.Template.g.cs
         * namespace: Glai.ECS.Core
         * container: public partial struct Archetype
         * maxArity: 7
         * slot: T
         * constraint: unmanaged, IComponent
         * public bool HasNone<{{GENERICS}}>()
         * {{WHERE_BLOCK}}
         * {
         *     return !({{#join T " || "}}HasComponent<{{TYPE_NAME}}>(){{/join}});
         * }
         */

        public int ChunkCount => chunkList.Count;

        public ref Chunk GetChunk(int index)
        {
            return ref chunkList.Get(index);
        }

        public int GetComponentStorageIndex(int componentTypeId)
        {
            for (int i = 0; i < componentCount; i++)
            {
                if (componentTypeIds[i] == componentTypeId)
                    return componentStorageMap[i]; // -1 for zero-size tags
            }
            return -1;
        }

        public int ComponentCount => componentCount;

        public int GetComponentTypeId(int index)
        {
            if (index < 0 || index >= componentCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return componentTypeIds[index];
        }

        public string GetDebugSignature()
        {
            if (componentCount == 0)
            {
                return "Empty";
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < componentCount; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(TypeRegistry.GetTypeName(componentTypeIds[i]));
            }

            return builder.ToString();
        }
        
        public bool Equals(Archetype other)
        {
            if (componentCount != other.componentCount)
                return false;

            for (int i = 0; i < componentCount; i++)
            {
                if (componentTypeIds[i] != other.componentTypeIds[i])
                    return false;
            }

            return true;
        }
    }
}
