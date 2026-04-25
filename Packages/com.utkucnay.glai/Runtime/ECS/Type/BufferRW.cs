using System;
using System.Runtime.CompilerServices;

namespace Glai.ECS
{
    public unsafe readonly struct BufferRW<T> where T : unmanaged, IBufferComponent
    {
        readonly int* lengthPtr;
        readonly T* elementsPtr;
        readonly int capacity;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferRW(void* slotPtr, int capacity)
        {
            lengthPtr = (int*)slotPtr;
            elementsPtr = (T*)((byte*)slotPtr + Core.BufferTypeInfo<T>.HeaderSize);
            this.capacity = capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferRW(IntPtr slotPtr, int capacity)
        {
            lengthPtr = (int*)slotPtr;
            elementsPtr = (T*)((byte*)slotPtr + Core.BufferTypeInfo<T>.HeaderSize);
            this.capacity = capacity;
        }

        public int Length => *lengthPtr;
        public int Capacity => capacity;

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)*lengthPtr)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return ref elementsPtr[index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(in T item)
        {
            int length = *lengthPtr;
            if (length >= capacity) return false;
            elementsPtr[length] = item;
            *lengthPtr = length + 1;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T item)
        {
            if (!TryAdd(item))
                throw new InvalidOperationException($"BufferRW<{typeof(T).Name}> is full (capacity={capacity}).");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index)
        {
            int length = *lengthPtr;
            if ((uint)index >= (uint)length)
                throw new ArgumentOutOfRangeException(nameof(index));

            int last = length - 1;
            if (index != last)
                elementsPtr[index] = elementsPtr[last];
            *lengthPtr = last;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() => *lengthPtr = 0;
    }
}
