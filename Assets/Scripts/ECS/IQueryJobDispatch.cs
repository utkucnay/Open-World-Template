using Glai.Allocator;
using Glai.ECS.Core;
using Unity.Jobs;

namespace Glai.ECS
{
    public unsafe interface ISystemDispatch
    {
        /// <summary>
        /// Allocates per-chunk data in the arena for job scheduling.
        /// Called once before archetype iteration begins.
        /// </summary>
        void Prepare(MemoryStateHandle arenaHandle, ECSMemoryState memoryState, int maxChunks);

        /// <summary>
        /// Resolves component storage indices for this archetype.
        /// Returns false if the archetype does not contain all required component types.
        /// </summary>
        bool PrepareArchetype(ref Archetype archetype);

        /// <summary>
        /// Collects per-chunk pointers and entity count into the arena-allocated buffer.
        /// </summary>
        void CollectChunk(ref Chunk chunk, int chunkIndex);

        /// <summary>
        /// Creates and schedules the IJobParallelFor with the collected chunk data.
        /// Returns the Unity JobHandle for later completion.
        /// </summary>
        JobHandle ScheduleParallel(int chunkCount);
    }
}
