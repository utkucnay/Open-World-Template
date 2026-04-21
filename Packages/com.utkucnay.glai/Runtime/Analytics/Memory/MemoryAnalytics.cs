using System.Collections.Generic;
using Glai.Allocator;

namespace Glai.Analytics.Memory
{
    public static class MemoryAnalytics
    {
        private static LinkedList<IAllocator> collections;

        static MemoryAnalytics()
        {
            collections = new LinkedList<IAllocator>();
        }

        public static void RegisterAllocator(object allocator)
        {
            if (allocator is IAllocator validAllocator)
            {
                collections.AddLast(validAllocator);
            }
        }

        public static void UnregisterAllocator(object allocator)
        {
            if (allocator is IAllocator validAllocator)
            {
                collections.Remove(validAllocator);
            }
        }

        public static IReadOnlyCollection<IAllocator> GetCollections()
        {
            return collections;
        }

        public static void ResetPeaks()
        {
            foreach (IAllocator allocator in collections)
            {
                allocator?.ResetPeaks();
            }
        }
    }
}
