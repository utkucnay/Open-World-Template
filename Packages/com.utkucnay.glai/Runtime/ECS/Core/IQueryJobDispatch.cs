using Glai.Allocator;
using Glai.ECS.Core;
using Unity.Jobs;

namespace Glai.ECS
{
    public unsafe interface IQueryJobDispatch
    {
        void Prepare(MemoryStateHandle arenaHandle, ECSMemoryState memoryState, int maxChunks);

        bool PrepareArchetype(ref Archetype archetype);

        void CollectChunk(ref Chunk chunk, int chunkIndex);

        JobHandle ScheduleParallel(int chunkCount);

        void RunNonBurst(int chunkCount);
    }
}
