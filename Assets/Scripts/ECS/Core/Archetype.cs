using Glai.Collection;
using Glai.Mathematics;

namespace Glai.ECS.Core
{
    public class Archetype
    {
        private FixedList<Chunk> chunk;

        public Archetype()
        {
            chunk = new FixedList<Chunk>(1);
        }

        public void Add<T>() where T : unmanaged, IComponent
        {
            
        }

        public void Dispose()
        {
            chunk.Dispose();
        }
    }
}