using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Glai.Allocator;
using Glai.Collection;
using Glai.Core;
using Unity.Collections;

namespace Glai.ECS.Core
{
    public ref struct ChunkData
    {
        public FixedString128Bytes name;
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

        MemoryStateHandle memoryStateHandle;
        HandleArray entityIdHandle;
        HandleArray dataHandle;
        HandleArray componentSizesHandle;
        HandleArray componentOffsetsHandle;

        IntPtr dataPtr;
        int* entityIds;
        int* componentSizesPtr;
        int* componentOffsetsPtr;

        bool IsDisposed => dataPtr == IntPtr.Zero;

        public int EntityCount => entityCount;
        public int EntityCapacity => entityCapacity;

        public Chunk(in ChunkData data, MemoryStateHandle memoryStateHandle, MemoryState memoryState)
        {
            this.memoryStateHandle = memoryStateHandle;            
            name = data.name;
            componentCount = data.componentCount;
            capacityBytes = data.capacityBytes;
            entityCount = 0;

            var allocator = memoryState.Get<IAllocator>(memoryStateHandle);

            // Store per-component sizes
            componentSizesHandle = allocator.AllocateArray<int>(componentCount);
            var sizesSpan = allocator.GetArray<int>(componentSizesHandle);
            data.componentSizes.CopyTo(sizesSpan);
            componentSizesPtr = (int*)Unsafe.AsPointer(ref sizesSpan.GetPinnableReference());

            // Compute entity capacity from total bytes per entity
            int totalBytesPerEntity = 0;
            for (int i = 0; i < componentCount; i++)
            {
                totalBytesPerEntity += data.componentSizes[i];
            }

            entityCapacity = capacityBytes / totalBytesPerEntity;

            if (entityCapacity <= 0)
            {
                throw new ArgumentException($"Chunk {name} has invalid capacity. capacityBytes={capacityBytes}, componentCount={componentCount}, totalBytesPerEntity={totalBytesPerEntity}.");
            }

            // Compute per-component offsets
            componentOffsetsHandle = allocator.AllocateArray<int>(componentCount);
            var offsetsSpan = allocator.GetArray<int>(componentOffsetsHandle);
            componentOffsetsPtr = (int*)Unsafe.AsPointer(ref offsetsSpan.GetPinnableReference());

            int currentOffset = 0;
            for (int i = 0; i < componentCount; i++)
            {
                componentOffsetsPtr[i] = currentOffset;
                currentOffset += entityCapacity * componentSizesPtr[i];
            }

            // Allocate entity ids
            entityIdHandle = allocator.AllocateArray<int>(entityCapacity);
            entityIds = (int*)Unsafe.AsPointer(ref allocator.GetArray<int>(entityIdHandle).GetPinnableReference());

            // Allocate data buffer
            dataPtr = IntPtr.Zero;
            dataHandle = allocator.AllocateArray<byte>(capacityBytes);
            dataPtr = (IntPtr)Unsafe.AsPointer(ref allocator.GetArray<byte>(dataHandle).GetPinnableReference());
        }

        public void Dispose(MemoryState memoryState)
        {   
            if (IsDisposed)
            {
                Logger.LogWarning($"Chunk {name} is already disposed.");
                return;
            }
            
            var allocator = memoryState.Get<IAllocator>(memoryStateHandle);
            allocator.Deallocate(entityIdHandle);
            allocator.Deallocate(dataHandle);
            allocator.Deallocate(componentSizesHandle);
            allocator.Deallocate(componentOffsetsHandle);

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
            entityIds[index] = entityId;
            entityCount++;
            return index;
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
                    byte* regionBase = (byte*)dataPtr + componentOffsetsPtr[c];
                    Unsafe.CopyBlock(
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

            return ref Unsafe.AsRef<T>((void*)(dataPtr + componentOffsetsPtr[componentIndex] + index * componentSizesPtr[componentIndex]));
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
        internal int* GetEntityIds()
        {
            return entityIds;
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
