using System;
using Glai.Core;
using Glai.Allocator;
using System.Runtime.CompilerServices;

namespace Glai.Collection
{
    public unsafe struct FixedQueue<T> where T : unmanaged
    {
        int count;
        int head;
        int tail;
        HandleArray handle;
        MemoryStateHandle allocatorHandle;
        T* arrayPointer;

        public FixedQueue(int capacity, MemoryStateHandle allocatorHandle, MemoryState memoryState)
        {
            this.allocatorHandle = allocatorHandle;
            var allocator = memoryState.Get<IAllocator>(allocatorHandle);
            handle = allocator.AllocateArray<T>(capacity);
            arrayPointer = (T*)Unsafe.AsPointer(ref allocator.GetArray<T>(handle).GetPinnableReference());
            count = 0;
            head = 0;
            tail = 0;
        }

        public void Dispose(MemoryState memoryState)
        {
            var allocator = memoryState.Get<IAllocator>(allocatorHandle);
            allocator.Deallocate(handle);
            arrayPointer = null;
        }

        public void Enqueue(T value)
        {
            if (count == handle.Capacity)
            {
                throw new InvalidOperationException("Queue is full.");
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("Queue is not initialized.");
            }

            arrayPointer[tail] = value;
            tail = (tail + 1) % handle.Capacity;
            count++;
        }

        public T Dequeue()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Queue is empty.");
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("Queue is not initialized.");
            }

            T value = arrayPointer[head];
            head = (head + 1) % handle.Capacity;
            count--;
            return value;
        }

        public T Peek()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Queue is empty.");
            }

            if (arrayPointer == null)
            {
                throw new InvalidOperationException("Queue is not initialized.");
            }

            return arrayPointer[head];
        }

        public void Clear()
        {
            count = 0;
            head = 0;
            tail = 0;
        }
    }
}