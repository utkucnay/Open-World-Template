using System;
using System.Collections.Generic;
using Glai.Allocator;
using Glai.Core;

namespace Glai.Allocator
{
    public struct MemoryStateHandle
    {
        public Guid Id { get; private set; }
        public int ArrayIndex { get; private set; }
        
        public MemoryStateHandle(Guid guid, int arrayIndex)
        {
            Id = guid;
            ArrayIndex = arrayIndex;
        }
    }

    public abstract class MemoryState : Glai.Core.Object
    {
        protected List<IAllocatorBase> Allocators { get; private set; }
        protected int AllocatorCount { get => Allocators.Count; }

        public MemoryState()
        {
            Allocators = new List<IAllocatorBase>(126);
        }

        public override void Dispose()
        {
            if (Disposed)
            {
                return;
            }
            
            for (int i = 0; i < Allocators.Count; i++)
            {
                Allocators[i].Dispose();
            }
            Allocators.Clear();
            base.Dispose();
        }

        protected MemoryStateHandle AddAllocator(IAllocatorBase allocator)
        {
            var handle = new MemoryStateHandle(Id, Allocators.Count);
            Allocators.Add(allocator);
            return handle;
        }

        public T Get<T>(MemoryStateHandle handle) where T : IAllocatorBase
        {
            if (handle.Id != Id)
            {
                LogError($"MemoryStateHandle with Id {handle.Id} does not belong to this MemoryState with Id {Id}.");
            }

            var allocator = Allocators[handle.ArrayIndex];
            if (allocator is T subAllocator)
            {
                return subAllocator;
            }

            LogError($"Allocator at index {handle.ArrayIndex} is not of type {typeof(T).Name}.");
            return default;            
        }
    }
}