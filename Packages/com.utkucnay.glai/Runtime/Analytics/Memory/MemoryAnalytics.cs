using System.Collections.Generic;
using Glai.Allocator;
using Glai.Core;
using UnityEngine;
using Object = Glai.Core.Object;

namespace Glai.Analytics.Memory
{
    public class MemoryAnalytics
    {
        private LinkedList<IAllocator> collections;

        public MemoryAnalytics()
        {
            collections = new LinkedList<IAllocator>();

            EventBus.Subscribe(IAllocator.RegisterEvent, RegisterAllocator);
            EventBus.Subscribe(IAllocator.UnregisterEvent, UnregisterAllocator);
        }

        ~MemoryAnalytics()
        {
            EventBus.Unsubscribe(IAllocator.RegisterEvent, RegisterAllocator);
            EventBus.Unsubscribe(IAllocator.UnregisterEvent, UnregisterAllocator);
        }

        public void RegisterAllocator(Object allocator)
        {
            if (allocator is IAllocator validAllocator)
            {
                collections.AddLast(validAllocator);
            }
        }

        public void UnregisterAllocator(Object allocator)
        {
            if (allocator is IAllocator validAllocator)
            {
                collections.Remove(validAllocator);
            }
        }

        public IReadOnlyCollection<IAllocator> GetCollections()
        {
            return collections;
        }

        public void ResetPeaks()
        {
            foreach (IAllocator allocator in collections)
            {
                allocator?.ResetPeaks();
            }
        }
    }
}
