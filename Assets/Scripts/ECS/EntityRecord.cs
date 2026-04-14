using System;

namespace Glai.ECS
{
    public struct EntityRecord : IEquatable<EntityRecord>
    {
        public int ArchetypeIndex;
        public int ChunkIndex;
        public int ComponentIndex;
    
        public bool Equals(EntityRecord other)
        {
            return ArchetypeIndex == other.ArchetypeIndex &&
                   ChunkIndex == other.ChunkIndex &&
                   ComponentIndex == other.ComponentIndex;
        }
    }
}