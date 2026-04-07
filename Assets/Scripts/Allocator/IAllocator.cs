using System;
using Glai.Core;
using Unity.Collections;

namespace Glai.Allocator
{
    public interface IAllocatorBase : IDisposable
    {
        FixedString128Bytes Name { get; }
        int Count { get; }
        int Capacity { get; }
    }

    public interface IAllocator : IAllocatorBase 
    {
        Handle Allocate<T>() where T : unmanaged;
        HandleArray AllocateArray<T>(int capacity) where T : unmanaged;

        void Deallocate(in Handle handle);
        void Deallocate(in HandleArray handle);

        T Get<T>(in Handle handle) where T : unmanaged;
        Span<T> GetArray<T>(in HandleArray handle) where T : unmanaged;

        void Set<T>(in Handle handle, T value) where T : unmanaged;
        void SetArray<T>(in HandleArray handle, in Span<T> values, int offset = 0) where T : unmanaged;
    }
}