using System;
using Glai.Core;
using Glai.Allocator;
using Glai.Collection;
using System.Runtime.CompilerServices;

namespace Glai.Tween.Core
{
    internal struct SequenceTween : IEquatable<SequenceTween>
    {
        public TweenHandle tween;
        public FixedArray<TweenHandle> concurrentTweens;

        public SequenceTween(TweenHandle tween, int concurrentTweenCount, MemoryStateHandle allocatorHandle, MemoryState memoryState)
        {
            this.tween = tween;
            this.concurrentTweens = new FixedArray<TweenHandle>(concurrentTweenCount, allocatorHandle, memoryState);
        }

        public bool Equals(SequenceTween other)
        {
            return tween.Equals(other.tween) && concurrentTweens.Equals(other.concurrentTweens);
        }
    }

    internal struct Sequence : IEquatable<Sequence>
    {
        public FixedList<SequenceTween> tweens;
        public int index;
        public MemoryStateHandle allocatorHandle;

        public Sequence(int capacity, MemoryStateHandle allocatorHandle, MemoryState memoryState)
        {
            this.allocatorHandle = allocatorHandle;
            index = 0;
            tweens = new FixedList<SequenceTween>(capacity, allocatorHandle, memoryState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(TweenHandle tween, MemoryState memoryState)
        {
            var sequenceTween = new SequenceTween(tween, 0, allocatorHandle, memoryState);
            tweens.Add(sequenceTween);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Span<TweenHandle> concurrentTweens, MemoryState memoryState)
        {
            var sequenceTween = new SequenceTween(concurrentTweens[0], concurrentTweens.Length - 1, allocatorHandle, memoryState);
            for (int i = 1; i < concurrentTweens.Length; i++)
            {
                sequenceTween.concurrentTweens[i - 1] = concurrentTweens[i];
            }
            tweens.Add(sequenceTween);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryIncreaseSequenceIndex()
        {
            index++;
            return index < tweens.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose(MemoryState memoryState)
        {
            tweens.Dispose(memoryState);
        }

        public bool Equals(Sequence other)
        {
            return tweens.Equals(other.tweens) && index == other.index && allocatorHandle.Equals(other.allocatorHandle);
        }
    }
}
