using Glai.Allocator;
using Glai.Core;
using Unity.Collections.LowLevel.Unsafe;

namespace Glai.Collection
{
    public unsafe class FixedBitArray
    {
        private HandleArray handle;
        private MemoryStateHandle allocatorHandle;

        private int _length;
        private int* _array;

        public FixedBitArray(int capacity, in MemoryStateHandle allocatorHandle, MemoryState memoryState)
        {
            this.allocatorHandle = allocatorHandle;
            
            var allocator = memoryState.Get<IAllocator>(allocatorHandle);
            handle = allocator.AllocateArray<int>((capacity + 31) / 32);
            
            _array = (int*)UnsafeUtility.AddressOf(ref allocator.GetArray<int>(handle).GetPinnableReference());
            _length = capacity;
        }

        public void Dispose(MemoryState memoryState)
        {
            if (_array == null)
            {
                Logger.LogWarning("Bit array is not initialized or already disposed.");
                return;
            }

            var allocator = memoryState.Get<IAllocator>(allocatorHandle);
            allocator.Deallocate(handle);
            _array = null;
        }

        public bool this[int index]
        {
            get
            {
                if (index < 0 || index >= _length)
                    throw new System.IndexOutOfRangeException();
                int arrayIndex = index / 32;
                int bitIndex = index % 32;
                return (_array[arrayIndex] & (1 << bitIndex)) != 0;
            }
            set
            {
                if (index < 0 || index >= _length)
                    throw new System.IndexOutOfRangeException();
                int arrayIndex = index / 32;
                int bitIndex = index % 32;
                if (value)
                    _array[arrayIndex] |= (1 << bitIndex);
                else
                    _array[arrayIndex] &= ~(1 << bitIndex);
            }
        }

        public int Length => _length;
    }
}
