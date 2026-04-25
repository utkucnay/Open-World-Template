using System;
using System.Runtime.CompilerServices;

namespace Glai.ECS
{
    public unsafe readonly struct BufferR<T> where T : unmanaged, IBufferComponent
    {
        readonly int* lengthPtr;
        readonly T* elementsPtr;
        readonly int capacity;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferR(void* slotPtr, int capacity)
        {
            lengthPtr = (int*)slotPtr;
            elementsPtr = (T*)((byte*)slotPtr + Core.BufferTypeInfo<T>.HeaderSize);
            this.capacity = capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferR(IntPtr slotPtr, int capacity)
        {
            lengthPtr = (int*)slotPtr;
            elementsPtr = (T*)((byte*)slotPtr + Core.BufferTypeInfo<T>.HeaderSize);
            this.capacity = capacity;
        }

        public int Length => *lengthPtr;
        public int Capacity => capacity;

        public ref readonly T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)*lengthPtr)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return ref elementsPtr[index];
            }
        }
    }
}
