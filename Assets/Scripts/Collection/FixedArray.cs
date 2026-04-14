using System;
using Glai.Core;
using Glai.Allocator;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Glai.Collection
{
    public unsafe struct FixedArray<T> where T : unmanaged, IEquatable<T>
    {
        HandleArray handle;
        MemoryStateHandle allocatorHandle;
        T* arrayPointer;

        public int Length => handle.Capacity;
        public int Capacity => handle.Capacity;

        public FixedArray(int capacity, in MemoryStateHandle allocatorHandle, MemoryState memoryState)
        {
            this.allocatorHandle = allocatorHandle;
            var allocator = memoryState.Get<IAllocator>(allocatorHandle);
            handle = allocator.AllocateArray<T>(capacity);
            arrayPointer = (T*)Unsafe.AsPointer(ref allocator.GetArray<T>(handle).GetPinnableReference());
        }

        public FixedArray(int capacity, Span<T> values, in MemoryStateHandle allocatorHandle, MemoryState memoryState)
        {
            this.allocatorHandle = allocatorHandle;
            var allocator = memoryState.Get<IAllocator>(allocatorHandle);
            handle = allocator.AllocateArray<T>(capacity);
            arrayPointer = (T*)Unsafe.AsPointer(ref allocator.GetArray<T>(handle).GetPinnableReference());
            
            Unsafe.CopyBlock(arrayPointer, Unsafe.AsPointer(ref values.GetPinnableReference()), (uint)(capacity * sizeof(T)));
        }

        public void Dispose(MemoryState memoryState)
        {
            if (arrayPointer == null)
            {
                throw new InvalidOperationException("Array is not initialized.");
            }

            var allocator = memoryState.Get<IAllocator>(allocatorHandle);
            allocator.Deallocate(handle);
            arrayPointer = null;
        }

        public T Get(int index)
        {
            if (index < 0 || index >= handle.Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("Array is not initialized.");
            }

            return arrayPointer[index];
        }

        public void Set(int index, in T value)
        {
            if (index < 0 || index >= handle.Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("Array is not initialized.");
            }
            
            arrayPointer[index] = value;
        }

        public void Swap(int index1, int index2)
        {
            if (index1 < 0 || index1 >= handle.Capacity || index2 < 0 || index2 >= handle.Capacity)
            {
                throw new ArgumentOutOfRangeException(index1 < 0 || index1 >= handle.Capacity ? nameof(index1) : nameof(index2));
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("Array is not initialized.");
            }

            T temp = arrayPointer[index1];
            arrayPointer[index1] = arrayPointer[index2];
            arrayPointer[index2] = temp;
        }

        public int FindIndex(T value)
        {
            if (arrayPointer == null)
            {
                throw new InvalidOperationException("Array is not initialized.");
            }

            for (int i = 0; i < handle.Capacity; i++)
            {
                if (arrayPointer[i].Equals(value))
                {
                    return i;
                }
            }
            
            return -1; // Not found
        }

        public T this[int index]
        {
            get { return Get(index); }
            set { Set(index, value); }
        }

        public int Count
        {
            get { return handle.Capacity; }
        }
    }
}