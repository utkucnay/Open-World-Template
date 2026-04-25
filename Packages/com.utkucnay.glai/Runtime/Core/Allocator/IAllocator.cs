using System;
using Glai.Collections;
using Glai.Core;

namespace Glai.Allocator
{
    public interface IAllocator : IDisposable
    {
        static string RegisterEvent => "RegisterEvent";
        static string UnregisterEvent => "UnregisterEvent";

        FixedString128Bytes Name { get; }
        
        int Count { get; }
        int PeakCount { get; }
        
        int HandleCount { get; }
        int PeakHandleCount { get; }
        
        int Capacity { get; }
        int HandleCapacity { get; }

        void ResetPeaks();

        Handle Allocate<T>(int alignment = 0) where T : unmanaged;
        HandleArray AllocateArray<T>(int capacity, int alignment = 0) where T : unmanaged;

        void Deallocate(in Handle handle);
        void Deallocate(in HandleArray handle);

        T Get<T>(in Handle handle) where T : unmanaged;
        Span<T> GetArray<T>(in HandleArray handle) where T : unmanaged;

        void Set<T>(in Handle handle, T value) where T : unmanaged;
        void SetArray<T>(in HandleArray handle, in Span<T> values, int offset = 0) where T : unmanaged;
    }
}