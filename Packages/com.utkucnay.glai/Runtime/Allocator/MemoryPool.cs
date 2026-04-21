using Glai.Core;
using Unity.Collections.LowLevel.Unsafe;

namespace Glai.Allocator
{
    public unsafe class MemoryPool : Object
    {
        byte* poolPointer;
        byte* offsetPointer;

        public MemoryPool(int capacicty)
        {
            poolPointer = (byte*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<byte>() * capacicty, UnsafeUtility.AlignOf<int>(), Unity.Collections.Allocator.Persistent);
        }

        public override void Dispose()
        {
            if (poolPointer == null)
            {
                Logger.LogWarning("Memory pool is not initialized or already disposed.");
                return;
            }

            base.Dispose();
            UnsafeUtility.Free(poolPointer, Unity.Collections.Allocator.Persistent);
            poolPointer = null;
        }

        public byte* Allocate(int size, int alignment)
        {
            offsetPointer = poolPointer;
            byte* alignedPointer = (byte*)(((long)offsetPointer + alignment - 1) & ~(alignment - 1));
            offsetPointer = alignedPointer + size;
            return alignedPointer;
        }
    }
}