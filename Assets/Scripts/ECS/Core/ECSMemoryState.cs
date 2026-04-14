using Glai.Allocator;
using Glai.Collection;
using Glai.Mathematics;

namespace Glai.ECS.Core
{
    public class ECSMemoryState : MemoryState
    {
        public MemoryStateHandle persistHandle { get; private set; }

        private FixedStack<MemoryStateHandle> stackHandle;
        const int maxStackCount = 100;
        int createdStackCount;

        public ECSMemoryState()
        {
            persistHandle = AddAllocator(new Persist(new PersistData(){
                name = "ECS_Persist",
                capacityBytes = Math.MB(450),
                maxHandles = 1000
            }));

            stackHandle = new FixedStack<MemoryStateHandle>(maxStackCount, persistHandle, this);
            createdStackCount = 0;
        }

        public override void Dispose()
        {
            if (Disposed) return;

            stackHandle.Dispose(this);
            base.Dispose();
        }

        public MemoryStateHandle PopStackHandle()
        {
            if (stackHandle.Count > 0)
            {
                return stackHandle.Pop();
            }

            if (createdStackCount < maxStackCount)
            {
                int stackIndex = createdStackCount;
                createdStackCount++;

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

        public void PushStackHandle(MemoryStateHandle handle)
        {
            stackHandle.Push(handle);
        }
    }
}
