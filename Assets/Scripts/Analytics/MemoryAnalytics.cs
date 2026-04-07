using System;
using System.Collections.Generic;
using Glai.Allocator;

namespace Glai.Analytics
{
    public static class MemoryAnalytics
    {
        private static LinkedList<IAllocatorBase> collections;

        static MemoryAnalytics()
        {
            collections = new LinkedList<IAllocatorBase>();
        }

        public static void RegisterCollection(IAllocatorBase collection)
        {
            collections.AddLast(collection);
        }

        public static void UnregisterCollection(IAllocatorBase collection)
        {
            collections.Remove(collection);
        }

        public static IReadOnlyCollection<IAllocatorBase> GetCollections()
        {
            return collections;
        }
    }
}
