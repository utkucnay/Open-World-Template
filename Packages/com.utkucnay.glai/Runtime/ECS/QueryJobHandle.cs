using Glai.Allocator;
using Glai.ECS.Core;
using Unity.Jobs;

namespace Glai.ECS
{
    public struct QueryJobHandle
    {
        internal JobHandle jobHandle;
        internal MemoryStateHandle arenaHandle;
        internal ECSMemoryState memoryState;

        internal QueryJobHandle(JobHandle jobHandle, MemoryStateHandle arenaHandle, ECSMemoryState memoryState)
        {
            this.jobHandle = jobHandle;
            this.arenaHandle = arenaHandle;
            this.memoryState = memoryState;
        }

        public void Complete()
        {
            jobHandle.Complete();
        }

        public void Dispose()
        {
            var arena = memoryState.Get<Arena>(arenaHandle);
            arena.Clear();
            memoryState.PushSystemArenaHandle(arenaHandle);
        }
    }
}
