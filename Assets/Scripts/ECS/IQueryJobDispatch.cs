using Glai.ECS.Core;

namespace Glai.ECS
{
    public unsafe interface IQueryJobDispatch
    {
        /// <summary>
        /// Resolves component storage indices for this archetype.
        /// Returns false if the archetype does not contain all required component types.
        /// </summary>
        bool PrepareArchetype(ref Archetype archetype);

        /// <summary>
        /// Iterates entities in the chunk and invokes the job's Execute method via a Burst-compiled static entry point.
        /// </summary>
        void ExecuteChunk(ref Chunk chunk);
    }
}
