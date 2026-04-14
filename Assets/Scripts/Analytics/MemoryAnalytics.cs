using System.Collections.Generic;
using Glai.Allocator.Core;

namespace Glai.Analytics
{
    public static class MemoryAnalytics
    {
        private static LinkedList<IAllocatorBase> collections;

        static MemoryAnalytics()
        {
            collections = new LinkedList<IAllocatorBase>();
        }

        public static void RegisterAllocator(IAllocatorBase allocator)
        {
            collections.AddLast(allocator);
        }

        public static void UnregisterAllocator(IAllocatorBase allocator)
        {
            collections.Remove(allocator);
        }

        public static IReadOnlyCollection<IAllocatorBase> GetCollections()
        {
            return collections;
        }
    }
}
