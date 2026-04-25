using System;
using Glai.Core;
using Unity.Collections.LowLevel.Unsafe;
using Object = Glai.Core.Object;

namespace Glai.Allocator
{
    internal unsafe struct PoolHeader
    {
        public int ByteSize;
        public byte IsFree;
        public IntPtr NextHeader;
        public IntPtr PrevHeader;
    }

    internal unsafe struct AllocationHeader
    {
        public PoolHeader* Block;
    }

    public unsafe class MemoryPool : Object
    {
        private byte* poolPointer;
        private readonly int capacity;

        public int Capacity => capacity;

        public int UsedBytes
        {
            get
            {
                if (poolPointer == null || Disposed)
                {
                    return 0;
                }

                int usedBytes = 0;
                for (PoolHeader* current = (PoolHeader*)poolPointer; current != null; current = (PoolHeader*)current->NextHeader)
                {
                    if (current->IsFree == 0)
                    {
                        usedBytes += current->ByteSize;
                    }
                }

                return usedBytes;
            }
        }

        public int FreeBytes
        {
            get
            {
                if (poolPointer == null || Disposed)
                {
                    return 0;
                }

                int freeBytes = 0;
                for (PoolHeader* current = (PoolHeader*)poolPointer; current != null; current = (PoolHeader*)current->NextHeader)
                {
                    if (current->IsFree != 0)
                    {
                        freeBytes += current->ByteSize;
                    }
                }

                return freeBytes;
            }
        }

        public MemoryPool(int capacity)
        {
            if (capacity <= sizeof(PoolHeader) + sizeof(AllocationHeader))
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Memory pool capacity is too small.");
            }

            this.capacity = capacity;
            poolPointer = (byte*)UnsafeUtility.Malloc(capacity, 16, Unity.Collections.Allocator.Persistent);
            UnsafeUtility.MemClear(poolPointer, capacity);

            var firstHeader = (PoolHeader*)poolPointer;
            firstHeader->ByteSize = capacity - sizeof(PoolHeader);
            firstHeader->IsFree = 1;
            firstHeader->NextHeader = IntPtr.Zero;
            firstHeader->PrevHeader = IntPtr.Zero;
        }

        public override void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            base.Dispose();

            if (poolPointer != null)
            {
                UnsafeUtility.Free(poolPointer, Unity.Collections.Allocator.Persistent);
                poolPointer = null;
            }
        }

        public byte* Allocate(int size, int alignment)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(nameof(MemoryPool));
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Allocation size must be greater than zero.");
            }

            if (alignment <= 0 || !AllocatorHelper.IsPowerOfTwo(alignment))
            {
                throw new ArgumentException("Alignment must be a positive power of two.", nameof(alignment));
            }

            PoolHeader* bestHeader = null;
            for (PoolHeader* current = (PoolHeader*)poolPointer; current != null; current = (PoolHeader*)current->NextHeader)
            {
                if (current->IsFree == 0)
                {
                    continue;
                }

                if (!TryGetRequiredBytes(current, size, alignment, out int requiredBytes))
                {
                    continue;
                }

                if (bestHeader == null || current->ByteSize < bestHeader->ByteSize)
                {
                    bestHeader = current;
                }
            }

            if (bestHeader == null)
            {
                throw new InvalidOperationException($"Memory pool is out of memory. Requested {size} bytes from a {capacity}-byte pool.");
            }

            byte* blockDataStart = (byte*)bestHeader + sizeof(PoolHeader);
            byte* userPointer = AllocatorHelper.AlignForward(blockDataStart + sizeof(AllocationHeader), alignment);
            int consumedBytes = (int)((userPointer + size) - blockDataStart);
            int remainingBytes = bestHeader->ByteSize - consumedBytes;

            if (remainingBytes >= sizeof(PoolHeader) + sizeof(AllocationHeader) + 1)
            {
                byte* nextHeaderPtr = blockDataStart + consumedBytes;
                var splitHeader = (PoolHeader*)nextHeaderPtr;
                splitHeader->ByteSize = remainingBytes - sizeof(PoolHeader);
                splitHeader->IsFree = 1;
                splitHeader->PrevHeader = (IntPtr)bestHeader;
                splitHeader->NextHeader = bestHeader->NextHeader;

                if (bestHeader->NextHeader != IntPtr.Zero)
                {
                    ((PoolHeader*)bestHeader->NextHeader)->PrevHeader = (IntPtr)splitHeader;
                }

                bestHeader->NextHeader = (IntPtr)splitHeader;
                bestHeader->ByteSize = consumedBytes;
            }

            bestHeader->IsFree = 0;

            var allocationHeader = (AllocationHeader*)(userPointer - sizeof(AllocationHeader));
            allocationHeader->Block = bestHeader;

            return userPointer;
        }

        public void Deallocate(void* pointer)
        {
            if (pointer == null || Disposed)
            {
                return;
            }

            var allocationHeader = (AllocationHeader*)((byte*)pointer - sizeof(AllocationHeader));
            PoolHeader* header = allocationHeader->Block;
            if (header == null)
            {
                return;
            }

            header->IsFree = 1;
            CoalesceWithNext(header);

            if (header->PrevHeader != IntPtr.Zero)
            {
                CoalesceWithNext((PoolHeader*)header->PrevHeader);
            }

            allocationHeader->Block = null;
        }

        private static bool TryGetRequiredBytes(PoolHeader* header, int size, int alignment, out int requiredBytes)
        {
            byte* dataStart = (byte*)header + sizeof(PoolHeader);
            byte* userPointer = AllocatorHelper.AlignForward(dataStart + sizeof(AllocationHeader), alignment);
            requiredBytes = (int)((userPointer + size) - dataStart);
            return requiredBytes <= header->ByteSize;
        }

        private static void CoalesceWithNext(PoolHeader* header)
        {
            if (header == null || header->IsFree == 0 || header->NextHeader == IntPtr.Zero)
            {
                return;
            }

            PoolHeader* nextHeader = (PoolHeader*)header->NextHeader;
            if (nextHeader->IsFree == 0)
            {
                return;
            }

            header->ByteSize += sizeof(PoolHeader) + nextHeader->ByteSize;
            header->NextHeader = nextHeader->NextHeader;

            if (nextHeader->NextHeader != IntPtr.Zero)
            {
                ((PoolHeader*)nextHeader->NextHeader)->PrevHeader = (IntPtr)header;
            }
        }
    }
}
