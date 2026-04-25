using Glai.Allocator;

namespace Glai
{
    public static class Global
    {
        private const int DefaultPoolCapacityBytes = 250 * 1024 * 1024;
        static MemoryPool defaultPool;

        public static MemoryPool CurrentDefaultPool => defaultPool;

        public static MemoryPool DefaultPool
        {
            get
            {
                if (defaultPool == null || defaultPool.Disposed)
                {
                    Initialize();
                }

                return defaultPool;
            }
        }

        public static void Initialize()
        {
            if (defaultPool != null && !defaultPool.Disposed)
            {
                defaultPool.Dispose();
            }

            defaultPool = new MemoryPool(DefaultPoolCapacityBytes);
        }

        public static void Dispose()
        {
            if (defaultPool == null)
            {
                return;
            }

            defaultPool.Dispose();
            defaultPool = null;
        }
    }   
}
