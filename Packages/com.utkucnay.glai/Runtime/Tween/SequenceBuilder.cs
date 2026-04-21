using System;
using Glai.Allocator;
using Glai.Collection;

namespace Glai.Tween
{
    public struct SequenceBuilder : IDisposable
    {
        internal FixedList<FixedList<TweenHandle>> tweens;
        internal MemoryStateHandle allocatorHandle;
        internal MemoryState memoryState;
        internal int index;
        internal int concurrentTweenCapacity;

        internal SequenceBuilder(int capacity, int concurrentTweenCapacity, in MemoryStateHandle allocatorHandle, MemoryState memoryState)
        {
            this.allocatorHandle = allocatorHandle;
            this.concurrentTweenCapacity = concurrentTweenCapacity;
            this.memoryState = memoryState;
            tweens = new FixedList<FixedList<TweenHandle>>(capacity, allocatorHandle, memoryState);
            index = -1;
        }

        public bool Append(TweenHandle tween)
        {
            index++;

            if (index >= tweens.Capacity)
            {
                return false;
            }

            if (index >= tweens.Count)
            {
                tweens.Add(new FixedList<TweenHandle>(concurrentTweenCapacity, allocatorHandle, memoryState));
                tweens.Get(index).Add(tween);
            }
            else
            {
                tweens.Get(index).Add(tween);
            }

            TweenManager.Instance.SetTweenActive(tween, index == 0);

            return true;
        }

        public bool Join(TweenHandle tween)
        {
            if (index < 0)
            {
                index = 0;
                tweens.Add(new FixedList<TweenHandle>(concurrentTweenCapacity, allocatorHandle, memoryState));
            }

            if (index >= tweens.Count)
            {
                tweens.Add(new FixedList<TweenHandle>(concurrentTweenCapacity, allocatorHandle, memoryState));
                tweens.Get(index).Add(tween);
            }
            else
            {
                tweens.Get(index).Add(tween);
            }

            TweenManager.Instance.SetTweenActive(tween, index == 0);

            return true;
        }

        public void Dispose()
        {
            TweenManager.Instance.AddSequence(this);

            for (int i = 0; i < tweens.Count; i++)
            {
                var tweenList = tweens.Get(i);
                tweenList.Dispose(memoryState);
            }
            tweens.Dispose(memoryState);
        }
    }
}