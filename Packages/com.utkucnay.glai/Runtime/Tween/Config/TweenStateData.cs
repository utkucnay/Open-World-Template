using System;
using Glai.Core;

namespace Glai.Tween.Core
{
    [Serializable]
    public struct TweenStateData
    {
        public string TweenPersistName;
        public ByteSize TweenPersistCapacityBytes;
        public int TweenPersistMaxHandles;
        public string TweenSequenceArenaName;
        public int TweenSequenceArenaAlignmentBytes;
        public ByteSize TweenSequenceArenaCapacityBytes;
        public int TweenSequenceArenaMaxHandles;
        public int TweenSequenceArenaCount;

        public static TweenStateData Default => new TweenStateData
        {
            TweenPersistName = "TweenState",
            TweenPersistCapacityBytes = ByteSize.MB(10),
            TweenPersistMaxHandles = 100,
            TweenSequenceArenaName = "TweenSequenceArena",
            TweenSequenceArenaAlignmentBytes = 0,
            TweenSequenceArenaCapacityBytes = ByteSize.KB(16),
            TweenSequenceArenaMaxHandles = 100,
            TweenSequenceArenaCount = 100,
        };
    }
}
