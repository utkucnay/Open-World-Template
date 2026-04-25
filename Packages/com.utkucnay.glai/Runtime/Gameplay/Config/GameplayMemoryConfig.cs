using System;
using Glai.Core;

namespace Glai.Gameplay.Core
{
    [Serializable]
    public struct GameplayMemoryConfig
    {
        public ByteSize PersistCapacityBytes;
        public int PersistMaxHandles;

        public static GameplayMemoryConfig Default => new GameplayMemoryConfig
        {
            PersistCapacityBytes = ByteSize.MB(16),
            PersistMaxHandles = 100,
        };
    }
}
