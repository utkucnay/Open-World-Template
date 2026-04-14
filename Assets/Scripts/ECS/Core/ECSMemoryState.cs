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
        const int maxStackCount = 100;
        int createdChunkStackCount;
        int createdQueryBuilderStackCount;

        public ECSMemoryState()
        {
            persistHandle = AddAllocator(new Persist(new PersistData(){
                name = "ECS_Persist",
                capacityBytes = Math.MB(10),
                maxHandles = 1000
            }));

            chunkStackHandles = new FixedStack<MemoryStateHandle>(maxStackCount, persistHandle, this);
            queryBuilderHandles = new FixedStack<MemoryStateHandle>(maxStackCount, persistHandle, this);
            createdChunkStackCount = 0;
            createdQueryBuilderStackCount = 0;
        }

        public override void Dispose()
        {
            if (Disposed) return;

            chunkStackHandles.Dispose(this);
            queryBuilderHandles.Dispose(this);
            base.Dispose();
        }

        public MemoryStateHandle PopChunkStackHandle()
        {
            if (chunkStackHandles.Count > 0)
            {
                return chunkStackHandles.Pop();
            }

            if (createdChunkStackCount < maxStackCount)
            {
                int stackIndex = createdChunkStackCount;
                createdChunkStackCount++;

                return AddAllocator(new Stack(new StackData()
                {
                    name = $"ECS_Stack_{stackIndex}",
                    capacityBytes = Math.MB(16),
                    maxHandles = 1000000
                }));
            }

            LogError("No more stack allocators available in ECSMemoryState.");
            return default;
        }

        public void PushChunkStackHandle(MemoryStateHandle handle)
        {
            chunkStackHandles.Push(handle);
        }

        public MemoryStateHandle PopQueryBuilderHandle()
        {
            if (queryBuilderHandles.Count > 0)
            {
                return queryBuilderHandles.Pop();
            }

            if (createdQueryBuilderStackCount < maxStackCount)
            {
                int stackIndex = createdQueryBuilderStackCount;
                createdQueryBuilderStackCount++;

                return AddAllocator(new Stack(new StackData()
                {
                    name = $"ECS_QueryBuilder_{stackIndex}",
                    capacityBytes = Math.KB(32),
                    maxHandles = 1000000
                }));
            }

            LogError("No more query builder allocators available in ECSMemoryState.");
            return default;
        }

        public void PushQueryBuilderHandle(MemoryStateHandle handle)
        {
            queryBuilderHandles.Push(handle);
        }
    }
}
