using System;
using System.Runtime.CompilerServices;
using Glai.Allocator;
using Glai.Collections;
using Glai.Core;
using Unity.Collections.LowLevel.Unsafe;

namespace Glai.ECS.Core
{
    public ref struct ChunkData
    {
        public FixedString128Bytes name;
        public int alignment;
        public int capacityBytes;
        public int componentCount;
        public Span<int> componentSizes;
    }

    public unsafe struct Chunk : IEquatable<Chunk>    
    {
        FixedString128Bytes name;

        int componentCount;
        int capacityBytes;
        int entityCount;
        int entityCapacity;

        MemoryStateHandle chunkStateHandle;
        MemoryStateHandle persistHandle;
        HandleArray entityIdHandle;
        HandleArray dataHandle;
        //TODO: Why we need to store component sizes and offsets in separate arrays instead of computing on the fly from archetype metadata?
        HandleArray componentSizesHandle;
        HandleArray componentOffsetsHandle;

        IntPtr dataPtr;
        int* entityIds;
        int* componentSizesPtr;
        int* componentOffsetsPtr;

        bool IsDisposed => dataPtr == IntPtr.Zero;

        public int EntityCount => entityCount;
        public int EntityCapacity => entityCapacity;

        public Chunk(in ChunkData data, MemoryStateHandle persistHandle, MemoryStateHandle chunkStackHandle, MemoryState memoryState)
        {
            this.persistHandle = persistHandle;
            this.chunkStateHandle = chunkStackHandle;
            name = data.name;
            componentCount = data.componentCount;
            capacityBytes = data.capacityBytes;
            entityCount = 0;

            var persistAllocator = memoryState.Get<IAllocator>(persistHandle);
            var chunkAllocator = memoryState.Get<IAllocator>(chunkStateHandle);

            // Store per-component sizes
            componentSizesHandle = persistAllocator.AllocateArray<int>(componentCount);
            var sizesSpan = persistAllocator.GetArray<int>(componentSizesHandle);
            data.componentSizes.CopyTo(sizesSpan);
            componentSizesPtr = (int*)UnsafeUtility.AddressOf(ref sizesSpan.GetPinnableReference());

            // Compute entity capacity from total bytes per entity
            int totalBytesPerEntity = 0;
            for (int i = 0; i < componentCount; i++)
            {
                totalBytesPerEntity += data.componentSizes[i];
            }

            entityCapacity = totalBytesPerEntity > 0
                ? capacityBytes / totalBytesPerEntity
                : capacityBytes / sizeof(int); // tag-only archetype: no data, use entity-id density

            if (entityCapacity <= 0)
            {
                throw new ArgumentException($"Chunk {name} has invalid capacity. capacityBytes={capacityBytes}, componentCount={componentCount}, totalBytesPerEntity={totalBytesPerEntity}.");
            }

            // Compute per-component offsets
            componentOffsetsHandle = persistAllocator.AllocateArray<int>(componentCount);
            var offsetsSpan = persistAllocator.GetArray<int>(componentOffsetsHandle);
            componentOffsetsPtr = (int*)UnsafeUtility.AddressOf(ref offsetsSpan.GetPinnableReference());

            int currentOffset = 0;
            for (int i = 0; i < componentCount; i++)
            {
                componentOffsetsPtr[i] = currentOffset;
                currentOffset += entityCapacity * componentSizesPtr[i];
            }

            // Allocate entity ids
            entityIdHandle = persistAllocator.AllocateArray<int>(entityCapacity);
            entityIds = (int*)UnsafeUtility.AddressOf(ref persistAllocator.GetArray<int>(entityIdHandle).GetPinnableReference());

            // Allocate data buffer
            dataPtr = IntPtr.Zero;
            dataHandle = chunkAllocator.AllocateArray<byte>(capacityBytes, data.alignment);
            dataPtr = (IntPtr)UnsafeUtility.AddressOf(ref chunkAllocator.GetArray<byte>(dataHandle).GetPinnableReference());
        }

        public void Dispose(MemoryState memoryState)
        {   
            if (IsDisposed)
            {
                Logger.LogWarning($"Chunk {name} is already disposed.");
                return;
            }

            var allocator = memoryState.Get<IAllocator>(persistHandle);
            var chunkAllocator = memoryState.Get<IAllocator>(chunkStateHandle);
            allocator.Deallocate(entityIdHandle);
            allocator.Deallocate(componentSizesHandle);
            allocator.Deallocate(componentOffsetsHandle);
            chunkAllocator.Deallocate(dataHandle);

            ((ECSMemoryState)memoryState).PushChunkStackHandle(chunkStateHandle);

            dataPtr = IntPtr.Zero;
            entityIds = null;
            componentSizesPtr = null;
            componentOffsetsPtr = null;
        }

        public int CreateSlot(int entityId)
        {
            if (entityCount >= entityCapacity)
            {
                throw new InvalidOperationException($"Chunk {name} is full.");
            }

            int index = entityCount;
            ClearSlot(index);
            entityIds[index] = entityId;
            entityCount++;
            return index;
        }

        private void ClearSlot(int index)
        {
            for (int c = 0; c < componentCount; c++)
            {
                int size = componentSizesPtr[c];
                if (size == 0) continue;

                byte* regionBase = (byte*)dataPtr + componentOffsetsPtr[c];
                UnsafeUtility.MemClear(regionBase + index * size, (uint)size);
            }
        }

        public int RemoveSlot(int index)
        {
            if (index < 0 || index >= entityCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Invalid slot index {index} for chunk {name}.");
            }

            int lastIndex = entityCount - 1;
            int swappedEntityId = -1;

            if (index != lastIndex)
            {
                for (int c = 0; c < componentCount; c++)
                {
                    int size = componentSizesPtr[c];
                    if (size == 0) continue; // tag component — no data to move
                    byte* regionBase = (byte*)dataPtr + componentOffsetsPtr[c];
                    UnsafeUtility.MemCpy(
                        regionBase + index * size,
                        regionBase + lastIndex * size,
                        (uint)size);
                }

                swappedEntityId = entityIds[lastIndex];
                entityIds[index] = swappedEntityId;
            }

            entityCount--;
            return swappedEntityId;
        }

        public ref T GetComponent<T>(int componentIndex, int index) where T : unmanaged
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException($"Chunk {name} is not allocated.");
            }

            if (componentIndex < 0 || componentIndex >= componentCount)
            {
                throw new ArgumentOutOfRangeException(nameof(componentIndex), $"Invalid component index {componentIndex} for chunk {name}.");
            }

            if (index < 0 || index >= entityCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Invalid entity index {index} for chunk {name}.");
            }

            return ref UnsafeUtility.AsRef<T>((void*)(dataPtr + componentOffsetsPtr[componentIndex] + index * componentSizesPtr[componentIndex]));
        }

        public (IntPtr, int) GetBuffer<T>(int componentIndex, int entityIndex) where T : unmanaged, IBufferComponent
        {
            if (IsDisposed)
                throw new InvalidOperationException($"Chunk {name} is not allocated.");
            if (componentIndex < 0 || componentIndex >= componentCount)
                throw new ArgumentOutOfRangeException(nameof(componentIndex));
            if (entityIndex < 0 || entityIndex >= entityCapacity)
                throw new ArgumentOutOfRangeException(nameof(entityIndex));

            void* slotPtr = (void*)(dataPtr
                + componentOffsetsPtr[componentIndex]
                + entityIndex * componentSizesPtr[componentIndex]);

            return (new IntPtr(slotPtr), BufferTypeInfo<T>.GetCapacity(componentSizesPtr[componentIndex]));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* GetComponentPtr(int componentStorageIndex)
        {
            return (byte*)dataPtr + componentOffsetsPtr[componentStorageIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetComponentSize(int componentStorageIndex)
        {
            return componentSizesPtr[componentStorageIndex];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int* GetEntityIds()
        {
            return entityIds;
        }

        public int GetEntityIdAt(int index)
        {
            if (index < 0 || index >= entityCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Invalid entity index {index} for chunk {name}.");
            }

            return entityIds[index];
        }

        public bool Equals(Chunk other)
        {
            return dataPtr == other.dataPtr;
        }

        public bool IsFull()
        {
            return entityCount >= entityCapacity;
        }
    }
}
