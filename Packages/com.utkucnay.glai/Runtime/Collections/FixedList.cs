using System;
using Glai.Core;
using Glai.Allocator;
using Unity.Collections.LowLevel.Unsafe;

namespace Glai.Collection
{
    public unsafe struct FixedList<T> : IEquatable<FixedList<T>> where T : unmanaged, IEquatable<T>
    {
        int count;
        HandleArray handle;
        MemoryStateHandle allocatorHandle;
        T* arrayPointer;

        public int Capacity => handle.Capacity;

        public FixedList(int capacity, in MemoryStateHandle allocatorHandle, MemoryState memoryState)
        {
            this.allocatorHandle = allocatorHandle;
            var allocator = memoryState.Get<IAllocator>(allocatorHandle);
            handle = allocator.AllocateArray<T>(capacity);
            arrayPointer = (T*)UnsafeUtility.AddressOf(ref allocator.GetArray<T>(handle).GetPinnableReference());
            count = 0;
        }

        public FixedList(int capacity, Span<T> values, in MemoryStateHandle allocatorHandle, MemoryState memoryState)
        {
            this.allocatorHandle = allocatorHandle;
            var allocator = memoryState.Get<IAllocator>(allocatorHandle);
            handle = allocator.AllocateArray<T>(capacity);
            arrayPointer = (T*)UnsafeUtility.AddressOf(ref allocator.GetArray<T>(handle).GetPinnableReference());
            UnsafeUtility.MemCpy(arrayPointer, UnsafeUtility.AddressOf(ref values.GetPinnableReference()), (uint)(values.Length * sizeof(T)));
            count = values.Length;
        }

        public void Dispose(MemoryState memoryState)
        {
            if (arrayPointer == null)
            {
                throw new InvalidOperationException("List is not initialized.");
            }

            var allocator = memoryState.Get<IAllocator>(allocatorHandle);
            allocator.Deallocate(handle);
            arrayPointer = null;
            count = 0;
        }

        public void Add(in T value)
        {
            if (count == handle.Capacity)
            {
                throw new InvalidOperationException("List is full.");
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("List is not initialized.");
            }

            arrayPointer[count] = value;
            count++;
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("List is not initialized.");
            }

            arrayPointer[index] = arrayPointer[count - 1];
            count--;
        }

        public ref T Get(int index)
        {
            if (index < 0 || index >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("List is not initialized.");
            }

            return ref arrayPointer[index];
        }

        public void Set(int index, in T value)
        {
            if (index < 0 || index >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("List is not initialized.");
            }

            arrayPointer[index] = value;
        }

        public void Swap(int index1, int index2)
        {
            if (index1 < 0 || index1 >= count || index2 < 0 || index2 >= count)
            {
                throw new ArgumentOutOfRangeException(index1 < 0 || index1 >= count ? nameof(index1) : nameof(index2));
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("List is not initialized.");
            }

            T temp = arrayPointer[index1];
            arrayPointer[index1] = arrayPointer[index2];
            arrayPointer[index2] = temp;
        }

        public bool Contains(in T value)
        {
            if (arrayPointer == null)
            {
                throw new InvalidOperationException("List is not initialized.");
            }
            for (int i = 0; i < count; i++)
            {
                if (arrayPointer[i].Equals(value))
                {
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            if (arrayPointer == null)
            {
                throw new InvalidOperationException("List is not initialized.");
            }

            count = 0;
        }

        public T this[int index]
        {
            get { return Get(index); }
            set { Set(index, in value); }
        }

        public int Count
        {
            get { return count; }
        }

        public Span<T> AsSpan()
        {
            if (arrayPointer == null)
            {
                throw new InvalidOperationException("List is not initialized.");
            }
            return new Span<T>(arrayPointer, count);
        }

        public bool Equals(FixedList<T> other)
        {
            return false; // Implementing equality for a mutable collection is complex and often not meaningful. Consider whether this is necessary for your use case.
        }
    }
}