using System;
using Glai.Core;

namespace Glai.ECS.Core
{
    [Serializable]
    public struct ECSMemoryConfig
    {
        public ByteSize PersistCapacityBytes;
        public int PersistMaxHandles;
        public int ChunkStackSize;
        public int QueryBuilderStackSize;
        public int SystemArenaSize;
        public int ChunkAllocatorAlignmentBytes;
        public ByteSize ChunkAllocatorCapacityBytes;
        public int ChunkAllocatorMaxHandles;
        public ByteSize QueryBuilderCapacityBytes;
        public int QueryBuilderMaxHandles;
        public int QueryBuilderTypeCapacity;
        public ByteSize SystemArenaCapacityBytes;
        public int SystemArenaMaxHandles;

        public static ECSMemoryConfig Default => new ECSMemoryConfig
        {
            PersistCapacityBytes = ByteSize.MB(10),
            PersistMaxHandles = 1000,
            ChunkStackSize = 500,
            QueryBuilderStackSize = 100,
            SystemArenaSize = 100,
            ChunkAllocatorAlignmentBytes = 64,
            ChunkAllocatorCapacityBytes = ByteSize.KB(32),
            ChunkAllocatorMaxHandles = 1,
            QueryBuilderCapacityBytes = ByteSize.KB(160),
            QueryBuilderMaxHandles = 100,
            QueryBuilderTypeCapacity = 16,
            SystemArenaCapacityBytes = ByteSize.KB(160),
            SystemArenaMaxHandles = 100,
        };
    }
}
