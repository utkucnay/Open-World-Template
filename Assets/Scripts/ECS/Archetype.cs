using System;
using Glai.Allocator;
using Glai.Collection;
using Glai.Core;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Glai.ECS.Core
{
    internal ref struct ArchetypeData
    {
        public int maxChunkCount;
        
        public int MaxComponentSize;

        public Span<int> ComponentTypeIds;
        private int componentTypeIndex;

        public ArchetypeData(int maxChunkCount, Span<int> componentTypeIds)
        {
            this.maxChunkCount = maxChunkCount;
            ComponentTypeIds = componentTypeIds;
            MaxComponentSize = 0;
            componentTypeIndex = 0;
        }

        public unsafe void AddComponent<T>() where T : unmanaged, IComponent
        {
            int componentTypeId = TypeId<T>.Id;

            for (int i = 0; i < componentTypeIndex; i++)
            {
                if (ComponentTypeIds[i] == componentTypeId)
                {
                    throw new InvalidOperationException($"Duplicate component type {typeof(T)} in archetype.");
                }
            }

            ComponentTypeIds[componentTypeIndex++] = componentTypeId;
            MaxComponentSize = math.max(MaxComponentSize, sizeof(T));
        }
    }

    internal struct Archetype : IEquatable<Archetype>
    {
        const int ChunkCapacityBytes = 16 * 1024;

        private FixedList<Chunk> chunkList;

        private FixedList<int> fullChunkIndices;
        private FixedStack<int> availableChunkIndices;

        private FixedArray<int> componentTypeIds;
        private FixedDictionary<int, int> componentTypeToIndex;
        private int maxComponentSize;
        private int componentCount;
        private MemoryStateHandle memoryStateHandle;

        public Archetype(in ArchetypeData archetypeData, MemoryStateHandle memoryStateHandle, MemoryState memoryState) 
        {
            this.memoryStateHandle = memoryStateHandle;
            maxComponentSize = archetypeData.MaxComponentSize;
            componentCount = archetypeData.ComponentTypeIds.Length;

            chunkList = new FixedList<Chunk>(archetypeData.maxChunkCount, memoryStateHandle, memoryState);            
            componentTypeIds = new FixedArray<int>(componentCount, archetypeData.ComponentTypeIds, memoryStateHandle, memoryState);
            componentTypeToIndex = new FixedDictionary<int, int>(componentCount * 2, memoryStateHandle, memoryState);
            fullChunkIndices = new FixedList<int>(archetypeData.maxChunkCount, memoryStateHandle, memoryState);
            availableChunkIndices = new FixedStack<int>(archetypeData.maxChunkCount, memoryStateHandle, memoryState);

            for (int i = 0; i < componentCount; i++)
            {
                componentTypeToIndex.Add(componentTypeIds[i], i);
            }

            chunkList.Add(CreateChunk(0, memoryState));

            availableChunkIndices.Push(0);
        }

        private Chunk CreateChunk(int index, MemoryState memoryState)
        {
            return new Chunk(new ChunkData
            {
                name = new FixedString128Bytes($"Chunk{index}"),
                capacityBytes = ChunkCapacityBytes,
                maxComponentSize = maxComponentSize,
                componentCount = componentCount
            }, memoryStateHandle, memoryState);
        }

        public EntityRecord AddEntity(MemoryState memoryState)
        {
            int chunkIndex;

            if (availableChunkIndices.Count == 0)
            {
                chunkIndex = chunkList.Count;
                chunkList.Add(CreateChunk(chunkIndex, memoryState));
                availableChunkIndices.Push(chunkIndex);
            }

            chunkIndex = availableChunkIndices.Pop();
            ref var chunk = ref chunkList.Get(chunkIndex);
            int componentIndex = chunk.CreateComponentIndex();

            if (chunk.IsFull())
            {
                fullChunkIndices.Add(chunkIndex);
            }
            else
            {
                availableChunkIndices.Push(chunkIndex);
            }

            return new EntityRecord()
            {
                ChunkIndex = chunkIndex,
                ComponentIndex = componentIndex
            };
        }

        public void RemoveEntity(EntityRecord entityRecord)
        {
            ref var chunk = ref chunkList.Get(entityRecord.ChunkIndex);
            bool wasFull = chunk.IsFull();
            chunk.RemoveComponentIndex(entityRecord.ComponentIndex);

            if (!chunk.IsFull() && !availableChunkIndices.Contains(entityRecord.ChunkIndex))
            {
                availableChunkIndices.Push(entityRecord.ChunkIndex);
            }

            if (wasFull)
            {
                for (int i = 0; i < fullChunkIndices.Count; i++)
                {
                    if (fullChunkIndices[i] == entityRecord.ChunkIndex)
                    {
                        fullChunkIndices.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        public int GetActiveChunk(int i)
        {
            if (i < fullChunkIndices.Count) return fullChunkIndices[i];
            i -= fullChunkIndices.Count;
            if (i < availableChunkIndices.Count) return availableChunkIndices[i];
            return -1;
        }

        public Span<T> GetComponents<T>(int chunkIndex) where T : unmanaged, IComponent
        {
            int componentIndex = GetComponentStorageIndex(TypeId<T>.Id);
            if (componentIndex == -1) throw new InvalidOperationException($"Component of type {typeof(T)} not found in archetype.");

            return chunkList[chunkIndex].GetComponents<T>(componentIndex);
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

        public bool ContainsComponentType(int componentTypeId)
        {
            return componentTypeToIndex.ContainsKey(componentTypeId);
        }

        public int ComponentCount => componentCount;

        private int GetComponentStorageIndex(int componentTypeId)
        {
            return componentTypeToIndex.TryGetValue(componentTypeId, out int componentIndex) ? componentIndex : -1;
        }

        public void Dispose(MemoryState memoryState)
        {
            for (int i = 0; i < chunkList.Count; i++)
            {
                chunkList[i].Dispose(memoryState);
            }

            chunkList.Dispose(memoryState);
            componentTypeIds.Dispose(memoryState);
            componentTypeToIndex.Dispose(memoryState);
            fullChunkIndices.Dispose(memoryState);
            availableChunkIndices.Dispose(memoryState);
        }

        public bool Equals(Archetype other)
        {
            if (componentCount != other.componentCount || maxComponentSize != other.maxComponentSize)
            {
                return false;
            }

            for (int i = 0; i < componentCount; i++)
            {
                if (componentTypeIds[i] != other.componentTypeIds[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
