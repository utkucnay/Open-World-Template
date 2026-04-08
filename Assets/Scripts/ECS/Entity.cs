using UnityEngine;

namespace Glai.ECS
{
    public struct Entity
    {
        public int Id { get; private set; }
        public int Generation { get; private set; }

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
    }
}
