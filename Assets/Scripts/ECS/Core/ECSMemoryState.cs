using Glai.Allocator;

namespace Glai.ECS.Core
{
    public class ECSMemoryState : MemoryState
    {
        MemoryStateHandle persistHandle;

        public ECSMemoryState()
        {
            persistHandle = AddAllocator(new Persist(new PersistData(){
                name = "ECS_Persist",
                capacityBytes = Math.MB(100)
            }));
        }
    }
}