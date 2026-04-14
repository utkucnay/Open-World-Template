using System;
using Glai.Allocator;
using Glai.Collection;
using Glai.Core;
using Math = Glai.Mathematics.Math;

namespace Glai.Tween.Core
{
    public struct TweenStateData
    {
        public PersistData tweenPersistData;
        public ArenaData tweenSequenceArenaData;
        public int tweenSequenceArenaCount;
    }

    public class TweenState : MemoryState
    {
        public MemoryStateHandle tweenPersistHandle;
        public FixedStack<MemoryStateHandle> tweenSequenceArenaHandle;

        public TweenState(TweenStateData data)
        {
            tweenPersistHandle = AddAllocator(new Persist(new PersistData
            {
                name = "TweenState",
                capacityBytes = Math.MB(10),
                maxHandles = 100
            }));

            tweenSequenceArenaHandle = new FixedStack<MemoryStateHandle>(100, tweenPersistHandle, this);
            for (int i = 0; i < 100; i++)
            {
                tweenSequenceArenaHandle.Push(AddAllocator(new Arena(new ArenaData
                {
                    name = $"TweenSequenceArena_{i}",
                    capacityBytes = Math.KB(16),
                    maxHandles = 100
                })));
            }
        }

        public MemoryStateHandle PopArenaHandle()
        {
            if (tweenSequenceArenaHandle.Count > 0)
            {
                var ret = tweenSequenceArenaHandle.Pop();
                Log($"Popping arena handle. Remaining count: {tweenSequenceArenaHandle.Count}");
                return ret;
            }

            LogError("No more arena available in tween state.");
            return default;
        }

        public void PushArenaHandle(MemoryStateHandle arenaHandle)
        {
            Log($"Pushing arena handle.");
            Get<Arena>(arenaHandle).Clear();
            tweenSequenceArenaHandle.Push(arenaHandle);
            Log($"Arena handle pushed. Remaining count: {tweenSequenceArenaHandle.Count}");
        }
    }
}