using Unity.Collections;
using Glai.Allocator;

namespace Glai.Gameplay.Core
{
    public class GameplayMemoryState : MemoryState
    {
        public MemoryStateHandle persistHandle;

        public GameplayMemoryState() : base()
        {
            persistHandle = AddAllocator(new Persist(new PersistData()
            {
                name = "GameplayMemoryState",
                capacityBytes = 16 * 1024 * 1024,
                maxHandles = 100
            }));
        }  
    }  
}