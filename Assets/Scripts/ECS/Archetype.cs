using System;
using Glai.Allocator;
using Glai.Collection;
using Glai.Core;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Glai.ECS.Core
{
    internal ref struct ArchetypeData
    {
        public int maxChunkCount;

        public Span<int> ComponentTypeIds;
        public Span<int> ComponentSizes;
        private int componentTypeIndex;

        public ArchetypeData(int maxChunkCount, Span<int> componentTypeIds, Span<int> componentSizes)
        {
            this.maxChunkCount = maxChunkCount;
            ComponentTypeIds = componentTypeIds;
            ComponentSizes = componentSizes;
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

            ComponentTypeIds[componentTypeIndex] = componentTypeId;
            ComponentSizes[componentTypeIndex] = sizeof(T);
            componentTypeIndex++;
        }
    }

    public struct Archetype : IEquatable<Archetype>
    {
        const int ChunkCapacityBytes = 16 * 1024;

        private FixedList<Chunk> chunkList;

        private FixedList<int> fullChunkIndices;
        private FixedStack<int> availableChunkIndices;

        private FixedArray<int> componentTypeIds;
        private FixedArray<int> componentSizes;
        private int componentCount;
        private MemoryStateHandle memoryStateHandle;

        internal Archetype(in ArchetypeData archetypeData, MemoryStateHandle memoryStateHandle, MemoryState memoryState) 
        {
            this.memoryStateHandle = memoryStateHandle;
            componentCount = archetypeData.ComponentTypeIds.Length;

            chunkList = new FixedList<Chunk>(archetypeData.maxChunkCount, memoryStateHandle, memoryState);            
            componentTypeIds = new FixedArray<int>(componentCount, archetypeData.ComponentTypeIds, memoryStateHandle, memoryState);
            componentSizes = new FixedArray<int>(componentCount, archetypeData.ComponentSizes, memoryStateHandle, memoryState);
            fullChunkIndices = new FixedList<int>(archetypeData.maxChunkCount, memoryStateHandle, memoryState);
            availableChunkIndices = new FixedStack<int>(archetypeData.maxChunkCount, memoryStateHandle, memoryState);

            chunkList.Add(CreateChunk(0, memoryState));
            availableChunkIndices.Push(0);
        }

        private Chunk CreateChunk(int index, MemoryState memoryState)
        {
            Span<int> sizes = stackalloc int[componentCount];
            for (int i = 0; i < componentCount; i++)
            {
                sizes[i] = componentSizes[i];
            }

            return new Chunk(new ChunkData
            {
                name = new FixedString128Bytes($"Chunk{index}"),
                capacityBytes = ChunkCapacityBytes,
                componentCount = componentCount,
                componentSizes = sizes
            }, memoryStateHandle, memoryState);
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
            fullChunkIndices.Dispose(memoryState);
            availableChunkIndices.Dispose(memoryState);
        }

        public EntityRecord AddEntity(MemoryState memoryState, int entityId)
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
            int componentIndex = chunk.CreateSlot(entityId);

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

        public int RemoveEntity(EntityRecord entityRecord)
        {
            ref var chunk = ref chunkList.Get(entityRecord.ChunkIndex);
            bool wasFull = chunk.IsFull();
            int swappedEntityId = chunk.RemoveSlot(entityRecord.ComponentIndex);

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
                        return swappedEntityId;
                    }
                }
            }

            return swappedEntityId;
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
            return GetComponentStorageIndex(TypeId<T>.Id) != -1;
        }

        public bool HasComponentTypeId(int componentTypeId)
        {
            return GetComponentStorageIndex(componentTypeId) != -1;
        }

        public bool HasAll<T1>()
            where T1 : unmanaged, IComponent
        {
            return HasComponent<T1>();
        }

        public bool HasAll<T1, T2>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            return HasComponent<T1>() && HasComponent<T2>();
        }

        public bool HasAll<T1, T2, T3>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
        {
            return HasComponent<T1>() && HasComponent<T2>() && HasComponent<T3>();
        }

        public bool HasAll<T1, T2, T3, T4>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
        {
            return HasComponent<T1>() && HasComponent<T2>() && HasComponent<T3>() && HasComponent<T4>();
        }

        public bool HasAll<T1, T2, T3, T4, T5>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
        {
            return HasComponent<T1>() && HasComponent<T2>() && HasComponent<T3>() && HasComponent<T4>() && HasComponent<T5>();
        }

        public bool HasAll<T1, T2, T3, T4, T5, T6>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
            where T6 : unmanaged, IComponent
        {
            return HasComponent<T1>() && HasComponent<T2>() && HasComponent<T3>() && HasComponent<T4>() && HasComponent<T5>() && HasComponent<T6>();
        }

        public bool HasAll<T1, T2, T3, T4, T5, T6, T7>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
            where T6 : unmanaged, IComponent
            where T7 : unmanaged, IComponent
        {
            return HasComponent<T1>() && HasComponent<T2>() && HasComponent<T3>() && HasComponent<T4>() && HasComponent<T5>() && HasComponent<T6>() && HasComponent<T7>();
        }

        public bool HasAny<T1>()
            where T1 : unmanaged, IComponent
        {
            return HasComponent<T1>();
        }

        public bool HasAny<T1, T2>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            return HasComponent<T1>() || HasComponent<T2>();
        }

        public bool HasAny<T1, T2, T3>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
        {
            return HasComponent<T1>() || HasComponent<T2>() || HasComponent<T3>();
        }

        public bool HasAny<T1, T2, T3, T4>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
        {
            return HasComponent<T1>() || HasComponent<T2>() || HasComponent<T3>() || HasComponent<T4>();
        }

        public bool HasAny<T1, T2, T3, T4, T5>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
        {
            return HasComponent<T1>() || HasComponent<T2>() || HasComponent<T3>() || HasComponent<T4>() || HasComponent<T5>();
        }

        public bool HasAny<T1, T2, T3, T4, T5, T6>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
            where T6 : unmanaged, IComponent
        {
            return HasComponent<T1>() || HasComponent<T2>() || HasComponent<T3>() || HasComponent<T4>() || HasComponent<T5>() || HasComponent<T6>();
        }

        public bool HasAny<T1, T2, T3, T4, T5, T6, T7>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
            where T6 : unmanaged, IComponent
            where T7 : unmanaged, IComponent
        {
            return HasComponent<T1>() || HasComponent<T2>() || HasComponent<T3>() || HasComponent<T4>() || HasComponent<T5>() || HasComponent<T6>() || HasComponent<T7>();
        }

        public bool HasNone<T1>()
            where T1 : unmanaged, IComponent
        {
            return !HasComponent<T1>();
        }

        public bool HasNone<T1, T2>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            return !HasAny<T1, T2>();
        }

        public bool HasNone<T1, T2, T3>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
        {
            return !HasAny<T1, T2, T3>();
        }

        public bool HasNone<T1, T2, T3, T4>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
        {
            return !HasAny<T1, T2, T3, T4>();
        }

        public bool HasNone<T1, T2, T3, T4, T5>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
        {
            return !HasAny<T1, T2, T3, T4, T5>();
        }

        public bool HasNone<T1, T2, T3, T4, T5, T6>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
            where T6 : unmanaged, IComponent
        {
            return !HasAny<T1, T2, T3, T4, T5, T6>();
        }

        public bool HasNone<T1, T2, T3, T4, T5, T6, T7>()
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
            where T6 : unmanaged, IComponent
            where T7 : unmanaged, IComponent
        {
            return !HasAny<T1, T2, T3, T4, T5, T6, T7>();
        }

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
                {
                    return i;
                }
            }
            return -1;
        }

        
        public bool Equals(Archetype other)
        {
            if (componentCount != other.componentCount)
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
