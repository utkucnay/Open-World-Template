using UnityEngine;
using System;

namespace Glai.ECS
{
    public struct Entity : IEquatable<Entity>
    {
        public int Id { get; private set; }
        internal int Generation { get; private set; }

        public Entity(int id)
        {
            Id = id;
            Generation = 0;
        }

        public Entity(int id, int generation)
        {
            Id = id;
            Generation = generation;
        }

        public bool Equals(Entity other)
        {
            return Id == other.Id && Generation == other.Generation;
        }
    }
}
