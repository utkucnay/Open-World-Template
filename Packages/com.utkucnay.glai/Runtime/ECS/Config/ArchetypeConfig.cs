using System;
using Glai.Core;

namespace Glai.ECS.Core
{
    [Serializable]
    public struct ArchetypeConfig
    {
        public ByteSize ChunkCapacityBytes;
        public int ChunkAlignmentBytes;
        public int MaxChunkCount;

        public static ArchetypeConfig Default => new ArchetypeConfig
        {
            ChunkCapacityBytes = ByteSize.KB(32),
            ChunkAlignmentBytes = 64,
            MaxChunkCount = 500,
        };
    }
}
