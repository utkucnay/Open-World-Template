using System;
using Glai.ECS.Core;

namespace Glai.ECS
{
    [Serializable]
    public struct EntityManagerConfig
    {
        public int MaxArchetypeCount;
        public int MaxEntityCount;
        public ECSMemoryConfig Memory;
        public ArchetypeConfig Archetype;

        public static EntityManagerConfig Default => new EntityManagerConfig
        {
            MaxArchetypeCount = 128,
            MaxEntityCount = 300_000,
            Memory = ECSMemoryConfig.Default,
            Archetype = ArchetypeConfig.Default,
        };
    }
}
