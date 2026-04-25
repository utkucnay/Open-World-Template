using Glai.Allocator;
using Glai.Collection;

namespace Glai.ECS.Core
{
    public class ECSMemoryState : MemoryState
    {
        readonly ECSMemoryConfig config;
        public ECSMemoryConfig Config => config;

        public MemoryStateHandle persistHandle { get; private set; }

        private FixedStack<MemoryStateHandle> chunkStackHandles;
        private FixedStack<MemoryStateHandle> queryBuilderHandles;
        private FixedStack<MemoryStateHandle> systemArenaHandles;

        private int createdChunkStackCount;
        private int createdQueryBuilderStackCount;
        private int createdSystemArenaCount;

        public ECSMemoryState() : this(ECSMemoryConfig.Default)
        {
        }

        public ECSMemoryState(ECSMemoryConfig config)
        {
            this.config = config;
            persistHandle = AddAllocator(new Persist(new PersistData(){
                name = "ECS_Persist",
                capacityBytes = config.PersistCapacityBytes.Bytes,
                maxHandles = config.PersistMaxHandles
            }));

            chunkStackHandles = new FixedStack<MemoryStateHandle>(config.ChunkStackSize, persistHandle, this);
            queryBuilderHandles = new FixedStack<MemoryStateHandle>(config.QueryBuilderStackSize, persistHandle, this);
            systemArenaHandles = new FixedStack<MemoryStateHandle>(config.SystemArenaSize, persistHandle, this);
        }

        private MemoryStateHandle CreateChunkStackHandle()
        {
            int index = createdChunkStackCount++;
            return AddAllocator(new Arena(new ArenaData()
            {
                name = $"ECS_ChunkStack_{index}",
                alignmentBytes = config.ChunkAllocatorAlignmentBytes,
                capacityBytes = config.ChunkAllocatorCapacityBytes.Bytes,
                maxHandles = config.ChunkAllocatorMaxHandles
            }));
        }

        private MemoryStateHandle CreateQueryBuilderHandle()
        {
            int index = createdQueryBuilderStackCount++;
            return AddAllocator(new Stack(new StackData()
            {
                name = $"ECS_QueryBuilderStack_{index}",
                capacityBytes = config.QueryBuilderCapacityBytes.Bytes,
                maxHandles = config.QueryBuilderMaxHandles
            }));
        }

        private MemoryStateHandle CreateSystemArenaHandle()
        {
            int index = createdSystemArenaCount++;
            return AddAllocator(new Arena(new ArenaData()
            {
                name = $"ECS_SystemArena_{index}",
                capacityBytes = config.SystemArenaCapacityBytes.Bytes,
                maxHandles = config.SystemArenaMaxHandles
            }));
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

            if (createdChunkStackCount < config.ChunkStackSize)
            {
                return CreateChunkStackHandle();
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

            if (createdQueryBuilderStackCount < config.QueryBuilderStackSize)
            {
                return CreateQueryBuilderHandle();
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

            if (createdSystemArenaCount < config.SystemArenaSize)
            {
                return CreateSystemArenaHandle();
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
