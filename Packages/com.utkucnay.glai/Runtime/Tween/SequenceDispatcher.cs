using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Glai.Allocator;
using Glai.Collection;
using Glai.Core;
using Glai.Tween.Core;
using Unity.Mathematics;
using UnityEngine;

namespace Glai.Tween
{
    internal class SequenceDispatcher
    {
        FixedList<Sequence> sequenceList;

        public SequenceDispatcher(int capacity, MemoryStateHandle allocatorHandle, MemoryState memoryState)
        {
            sequenceList = new FixedList<Sequence>(capacity, allocatorHandle, memoryState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose(MemoryState memoryState)
        {
            sequenceList.Dispose(memoryState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddSequence(Sequence sequence, ref TweenDispatch tweenDispatch)
        {
            ActiveTween(ref sequence, ref tweenDispatch);
            sequenceList.Add(sequence);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveSequence(int index)
        {
            sequenceList[index].Dispose(ITweenManager.Instance.TweenState);
            ITweenManager.Instance.TweenState.PushArenaHandle(sequenceList[index].allocatorHandle);
            sequenceList.RemoveAt(index);
        }

        public void BeforeDispatch(ref TweenDispatch tweenDispatch)
        {
            for (int i = 0; i < sequenceList.Count; i++)
            {
                ref Sequence sequence = ref sequenceList.Get(i);

                if (!IsActiveTweenSequence(ref sequence, ref tweenDispatch))
                {
                    if (sequence.TryIncreaseSequenceIndex())
                    {
                        ActiveTween(ref sequence, ref tweenDispatch);
                    }
                    else
                    {
                        RemoveSequence(i);
                        i--;
                    }
                }
            }
        }

        private void ActiveTween(ref Sequence sequence, ref TweenDispatch tweenDispatch)
        {
            var seqTween = sequence.tweens[sequence.index];
            var tweenHandle = seqTween.tween;
            
            switch (tweenHandle.Type)
            {
                case TweenType.Position:
                    tweenDispatch.positionDispatcher.SetActive(tweenHandle, true);
                    break;
                case TweenType.Rotation:
                    tweenDispatch.rotationDispatcher.SetActive(tweenHandle, true);
                    break;
                case TweenType.Scale:
                    tweenDispatch.scaleDispatcher.SetActive(tweenHandle, true);
                    break;
            }

            for (int i = 0; i < seqTween.concurrentTweens.Count; i++)
            {
                var concurrentTweenHandle = seqTween.concurrentTweens[i];

                switch (concurrentTweenHandle.Type)
                {
                    case TweenType.Position:
                        tweenDispatch.positionDispatcher.SetActive(concurrentTweenHandle, true);
                        break;
                    case TweenType.Rotation:
                        tweenDispatch.rotationDispatcher.SetActive(concurrentTweenHandle, true);
                        break;
                    case TweenType.Scale:
                        tweenDispatch.scaleDispatcher.SetActive(concurrentTweenHandle, true);
                        break;
                }
            }
        }

        private bool IsActiveTweenSequence(ref Sequence sequence, ref TweenDispatch tweenDispatch)
        {
            var seqTween = sequence.tweens[sequence.index];
            var tweenHandle = seqTween.tween;

            switch (tweenHandle.Type)
            {
                case TweenType.Position:
                    if (tweenDispatch.positionDispatcher.IsActive(tweenHandle))
                    {
                        return true;
                    }
                    break;
                case TweenType.Rotation:
                    if (tweenDispatch.rotationDispatcher.IsActive(tweenHandle))
                    {
                        return true;
                    }
                    break;
                case TweenType.Scale:
                    if (tweenDispatch.scaleDispatcher.IsActive(tweenHandle))
                    {
                        return true;
                    }
                    break;
            }

            for (int i = 0; i < seqTween.concurrentTweens.Count; i++)
            {
                var concurrentTweenHandle = seqTween.concurrentTweens[i];

                switch (concurrentTweenHandle.Type)
                {
                    case TweenType.Position:
                        if (tweenDispatch.positionDispatcher.IsActive(concurrentTweenHandle))
                        {
                            return true;
                        }
                        break;
                    case TweenType.Rotation:
                        if (tweenDispatch.rotationDispatcher.IsActive(concurrentTweenHandle))
                        {
                            return true;
                        }
                        break;
                    case TweenType.Scale:
                        if (tweenDispatch.scaleDispatcher.IsActive(concurrentTweenHandle))
                        {
                            return true;
                        }
                        break;
                }
            }

            return false;
        }
    }
}