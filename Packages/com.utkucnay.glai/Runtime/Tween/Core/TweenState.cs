using System;
using Glai.Allocator;
using Glai.Collection;
using Glai.Core;

namespace Glai.Tween.Core
{
    public class TweenState : MemoryState
    {
        public MemoryStateHandle tweenPersistHandle;
        public FixedStack<MemoryStateHandle> tweenSequenceArenaHandle;

        public TweenState(TweenStateData data)
        {
            data = ResolveData(data);
            tweenPersistHandle = AddAllocator(new Persist(new PersistData
            {
                name = data.TweenPersistName,
                capacityBytes = data.TweenPersistCapacityBytes.Bytes,
                maxHandles = data.TweenPersistMaxHandles,
            }));

            tweenSequenceArenaHandle = new FixedStack<MemoryStateHandle>(data.TweenSequenceArenaCount, tweenPersistHandle, this);
            for (int i = 0; i < data.TweenSequenceArenaCount; i++)
            {
                tweenSequenceArenaHandle.Push(AddAllocator(new Arena(new ArenaData
                {
                    name = $"{data.TweenSequenceArenaName}_{i}",
                    alignmentBytes = data.TweenSequenceArenaAlignmentBytes,
                    capacityBytes = data.TweenSequenceArenaCapacityBytes.Bytes,
                    maxHandles = data.TweenSequenceArenaMaxHandles,
                })));
            }
        }

        private static TweenStateData ResolveData(TweenStateData data)
        {
            var defaults = TweenStateData.Default;
            if (string.IsNullOrEmpty(data.TweenPersistName))
                data.TweenPersistName = defaults.TweenPersistName;
            if (data.TweenPersistCapacityBytes.Value <= 0)
                data.TweenPersistCapacityBytes = defaults.TweenPersistCapacityBytes;
            if (data.TweenPersistMaxHandles <= 0)
                data.TweenPersistMaxHandles = defaults.TweenPersistMaxHandles;
            if (string.IsNullOrEmpty(data.TweenSequenceArenaName))
                data.TweenSequenceArenaName = defaults.TweenSequenceArenaName;
            if (data.TweenSequenceArenaCapacityBytes.Value <= 0)
                data.TweenSequenceArenaCapacityBytes = defaults.TweenSequenceArenaCapacityBytes;
            if (data.TweenSequenceArenaMaxHandles <= 0)
                data.TweenSequenceArenaMaxHandles = defaults.TweenSequenceArenaMaxHandles;
            if (data.TweenSequenceArenaCount <= 0)
                data.TweenSequenceArenaCount = defaults.TweenSequenceArenaCount;
            return data;
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
