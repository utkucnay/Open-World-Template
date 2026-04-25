using Unity.Collections;
using Glai.Allocator;

namespace Glai.Gameplay.Core
{
    public class GameplayMemoryState : MemoryState
    {
        public MemoryStateHandle persistHandle;

        public GameplayMemoryState() : this(GameplayMemoryConfig.Default)
        {
        }

        public GameplayMemoryState(GameplayMemoryConfig config) : base()
        {
            persistHandle = AddAllocator(new Persist(new PersistData()
            {
                name = "GameplayMemoryState",
                capacityBytes = config.PersistCapacityBytes.Bytes,
                maxHandles = config.PersistMaxHandles
            }));
        }  
    }  
}
