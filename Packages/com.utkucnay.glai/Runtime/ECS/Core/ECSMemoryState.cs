using Glai.Allocator;
using Glai.Collection;
using Glai.Mathematics;

namespace Glai.ECS.Core
{
    public class ECSMemoryState : MemoryState
    {
        public MemoryStateHandle persistHandle { get; private set; }

        private FixedStack<MemoryStateHandle> chunkStackHandles;
        private FixedStack<MemoryStateHandle> queryBuilderHandles;
        private FixedStack<MemoryStateHandle> systemArenaHandles;

        public ECSMemoryState()
        {
            persistHandle = AddAllocator(new Persist(new PersistData(){
                name = "ECS_Persist",
                capacityBytes = Math.MB(10),
                maxHandles = 500
            }));

            var chunkStackSize = 200;
            var queryBuilderStackSize = 100; 
            var systemArenaSize = 100;

            chunkStackHandles = new FixedStack<MemoryStateHandle>(chunkStackSize, persistHandle, this);
            queryBuilderHandles = new FixedStack<MemoryStateHandle>(queryBuilderStackSize, persistHandle, this);
            systemArenaHandles = new FixedStack<MemoryStateHandle>(systemArenaSize, persistHandle, this);

            for (int i = 0; i < chunkStackSize; i++)
            {
                var chunkStackHandle = AddAllocator(new Arena(new ArenaData()
                {
                    name = $"ECS_ChunkStack_{i}",
                    capacityBytes = Math.KB(32),
                    maxHandles = 1
                }));
                chunkStackHandles.Push(chunkStackHandle);
            }
            for (int i = 0; i < queryBuilderStackSize; i++)
            {
                var queryBuilderHandle = AddAllocator(new Stack(new StackData()
                {
                    name = $"ECS_QueryBuilderStack_{i}",
                    capacityBytes = Math.KB(160),
                    maxHandles = 100
                }));
                queryBuilderHandles.Push(queryBuilderHandle);        
            }

            for (int i = 0; i < systemArenaSize; i++)
            {
                var systemArenaHandle = AddAllocator(new Arena(new ArenaData()
                {
                    name = $"ECS_SystemArena_{i}",
                    capacityBytes = Math.KB(160),
                    maxHandles = 100
                }));
                systemArenaHandles.Push(systemArenaHandle);
            }
        }

        public override void Dispose()
        {
            if (Disposed) return;

            chunkStackHandles.Dispose(this);
            queryBuilderHandles.Dispose(this);
            systemArenaHandles.Dispose(this);
            base.Dispose();
        }

        public MemoryStateHandle PopChunkStackHandle()
        {
            if (chunkStackHandles.Count > 0)
            {
                return chunkStackHandles.Pop();
            }

            LogError("No more stack allocators available in ECSMemoryState.");
            return default;
        }

        public void PushChunkStackHandle(MemoryStateHandle handle)
        {
            Get<Arena>(handle).Clear();
            chunkStackHandles.Push(handle);
        }

        public MemoryStateHandle PopQueryBuilderHandle()
        {
            if (queryBuilderHandles.Count > 0)
            {
                return queryBuilderHandles.Pop();
            }

            LogError("No more query builder allocators available in ECSMemoryState.");
            return default;
        }

        public void PushQueryBuilderHandle(MemoryStateHandle handle)
        {
            queryBuilderHandles.Push(handle);
        }

        public MemoryStateHandle PopSystemArenaHandle()
        {
            if (systemArenaHandles.Count > 0)
            {
                return systemArenaHandles.Pop();
            }

            LogError("No more system arena allocators available in ECSMemoryState.");
            return default;
        }

        public IAllocator GetAllocator(MemoryStateHandle handle)
        {
            return Get<IAllocator>(handle);
        }

        public void PushSystemArenaHandle(MemoryStateHandle handle)
        {
            systemArenaHandles.Push(handle);
        }
    }
}
