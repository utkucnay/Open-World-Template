using Glai.Allocator;
using Glai.ECS.Core;
using Unity.Jobs;

namespace Glai.ECS
{
    /// <summary>
    /// Returned by EntityManager.Run(). Bundles a Unity JobHandle with an arena allocator handle.
    /// The caller must call Complete() to wait for the job, then Dispose() to return resources to pools.
    /// </summary>
    public struct SystemHandle
    {
        internal JobHandle jobHandle;
        internal MemoryStateHandle arenaHandle;
        internal ECSMemoryState memoryState;
        internal QueryBuilder query;

        internal SystemHandle(JobHandle jobHandle, MemoryStateHandle arenaHandle, ECSMemoryState memoryState, QueryBuilder query)
        {
            this.jobHandle = jobHandle;
            this.arenaHandle = arenaHandle;
            this.memoryState = memoryState;
            this.query = query;
        }

        /// <summary>
        /// Blocks until the parallel job finishes. Must be called before reading results.
        /// </summary>
        public void Complete()
        {
            jobHandle.Complete();
        }

        /// <summary>
        /// Returns the arena allocator to the pool and disposes the query.
        /// Must be called after Complete().
        /// </summary>
        public void Dispose()
        {
            // Return arena to pool
            var arena = memoryState.Get<Arena>(arenaHandle);
            arena.Clear();
            memoryState.PushSystemArenaHandle(arenaHandle);

            // Dispose query
            if (!query.disposed)
            {
                query.noneTypeIds.Dispose(memoryState);
                query.anyTypeIds.Dispose(memoryState);
                query.allTypeIds.Dispose(memoryState);
                memoryState.PushQueryBuilderHandle(query.memoryStateHandle);
                query.disposed = true;
            }
        }
    }
}
