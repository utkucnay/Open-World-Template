using System;
using Glai.Core;
using Glai.Allocator;
using System.Runtime.CompilerServices;

namespace Glai.Collection
{
    public unsafe struct FixedStack<T> where T : unmanaged, IEquatable<T>
    {
        int count;
        HandleArray handle;
        MemoryStateHandle allocatorHandle;
        T* arrayPointer;

        public int Count => count;

        public FixedStack(int capacity, MemoryStateHandle allocatorHandle, MemoryState memoryState)
        {
            this.allocatorHandle = allocatorHandle;
            var allocator = memoryState.Get<IAllocator>(allocatorHandle);
            handle = allocator.AllocateArray<T>(capacity);
            arrayPointer = (T*)Unsafe.AsPointer(ref allocator.GetArray<T>(handle).GetPinnableReference());
            count = 0;
        }

        public void Dispose(MemoryState memoryState)
        {
            if (arrayPointer == null)
            {
                throw new InvalidOperationException("Stack is not initialized.");
            }

             var allocator = memoryState.Get<IAllocator>(allocatorHandle);
             allocator.Deallocate(handle);
             arrayPointer = null;
        }

        public void Push(T value)
        {
            if (count == handle.Capacity)
            {
                throw new InvalidOperationException($"Stack is full. Capacity: {handle.Capacity}, Count: {count}");
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("Stack is not initialized.");
            }

            arrayPointer[count] = value;
            count++;
        }

        public T Pop()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Stack is empty.");
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("Stack is not initialized.");
            }

            count--;
            return arrayPointer[count];
        }

        public T Peek()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Stack is empty.");
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("Stack is not initialized.");
            }
            
            return arrayPointer[count - 1];
        }

        public bool Contains(T value)
        {
            if (arrayPointer == null)
            {
                throw new InvalidOperationException("Stack is not initialized.");
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
            count = 0;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (arrayPointer == null)
                {
                    throw new InvalidOperationException("Stack is not initialized.");
                }

                return arrayPointer[index];
            }
        }
    }
}
