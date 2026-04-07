using Unity.Mathematics;
using System;
using UnityEngine;
using Glai.Collection;
using Unity.Collections;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Glai.Tween.Core;
using Unity.Burst;
using Logger = Glai.Core.Logger;
using System.ComponentModel;
using Glai.Allocator;

namespace Glai.Tween
{
    public enum Ease
    {
        Linear,
        EaseInQuad,
        EaseOutQuad,
        EaseInOutQuad,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        EaseInQuart,
        EaseOutQuart,
        EaseInOutQuart,
        EaseInQuint,
        EaseOutQuint,
        EaseInOutQuint
    } 

    [BurstCompile]
    internal unsafe static class TweenDispatcherBurst
    {
        [BurstCompile]
        public static void Dispatch(FixedList<Tween<float3>>* tweens, float globalSpeed, float deltaTime)
        {
            for (int i = 0; i < tweens->Count; i++)
            {
                ref var tween = ref tweens->Get(i);
                tween.IncreaseTime(deltaTime * globalSpeed);

                var value = EaseFunc(tween.FromValue, tween.ToValue, tween.CurrentTime / tween.Duration, Ease.Linear);

                if (tween.Target.targetType == TweenTarget.TargetType.Transform)
                {
                    if (tween.Target.propertyType == TweenTarget.PropertyType.Position)
                    {
                    }
                    else if (tween.Target.propertyType == TweenTarget.PropertyType.Scale)
                    {
                    }
                }
            }
        }

        public static float3 EaseFunc(in float3 from, in float3 to, float t, Ease ease)
        {
            return math.lerp(from, to, t);
        }
    }


    internal interface IDispatcher
    {
        void Dispatch(float deltaTime);
    }

    internal class TweenDispatcher<T> : TweenObject, IDispatcher where T : unmanaged
    {
        Guid guid;
        
        FixedList<Tween<T>> tweens;
        
        FixedArray<TweenHandle> tweenHandles;
        FixedList<int> freeHandleIndices;
        int nextTweenHandleIndex;

        Dictionary<int, Transform> transformMap;
        
        TweenType tweenType;
        
        Func<T, T, float, T> lerpFunction;

        public TweenDispatcher(
            TweenType tweenType, 
            Func<T, T, float, T> lerpFunction)
        {
            guid = Guid.NewGuid();
            transformMap = new Dictionary<int, Transform>();
            tweens = new FixedList<Tween<T>>(256, ITweenManager.Instance.TweenState.tweenPersistHandle, ITweenManager.Instance.TweenState);
            tweenHandles = new FixedArray<TweenHandle>(256, ITweenManager.Instance.TweenState.tweenPersistHandle, ITweenManager.Instance.TweenState);
            freeHandleIndices = new FixedList<int>(256, ITweenManager.Instance.TweenState.tweenPersistHandle, ITweenManager.Instance.TweenState);
            this.lerpFunction = lerpFunction;
            this.tweenType = tweenType;
        }

        public void Dispose(MemoryState memoryState)
        {
            tweens.Dispose(memoryState);
            tweenHandles.Dispose(memoryState);
            freeHandleIndices.Dispose(memoryState);
        }

        public TweenHandle AddTween(T fromValue, T toValue, float duration, TweenTarget target, FixedString128Bytes debugName)
        {
            var tween = new Tween<T>(fromValue, toValue, duration, target, debugName);

            if (freeHandleIndices.Count > 0)
            {
                int freeIndex = freeHandleIndices[freeHandleIndices.Count - 1];
                freeHandleIndices.RemoveAt(freeHandleIndices.Count - 1);
                tweens[tweenHandles[freeIndex].ArrayIndex] = tween;
                tweenHandles[freeIndex] = new TweenHandle(
                    guid, 
                    freeIndex, 
                    tweenHandles[freeIndex].ArrayIndex, 
                    tweenHandles[freeIndex].Generation, 
                    tweenType, 
                    true);
                return tweenHandles[freeIndex];
            }

            tweenHandles[nextTweenHandleIndex] = new TweenHandle(guid, nextTweenHandleIndex, tweens.Count, 0, tweenType, true);
            tweens.Add(tween);
            return tweenHandles[nextTweenHandleIndex++];
        }

        public void RemoveTween(TweenHandle handle)
        {
            if (handle.Index < 0 || handle.Index >= nextTweenHandleIndex)
            {
                throw new InvalidOperationException("Invalid tween handle.");
            }

            if (!handle.IsValid(tweenHandles[handle.Index]))
            {
                throw new InvalidOperationException("Invalid tween handle.");
            }

            tweens[handle.ArrayIndex] = default;
            tweenHandles[handle.Index] = new TweenHandle(
                    tweenHandles[handle.Index].Id, 
                    tweenHandles[handle.Index].Index, 
                    tweenHandles[handle.Index].ArrayIndex, 
                    tweenHandles[handle.Index].Generation + 1, 
                    tweenHandles[handle.Index].Type,
                    false);
            freeHandleIndices.Add(handle.Index);
        }

        public void SetTweenSpeed(TweenHandle handle, float speed)
        {
            if (handle.Index < 0 || handle.Index >= nextTweenHandleIndex)
            {
                throw new InvalidOperationException("Invalid tween handle.");
            }

            if (!handle.IsValid(tweenHandles[handle.Index]))
            {
                throw new InvalidOperationException("Invalid tween handle.");
            }

            var tween = tweens[handle.ArrayIndex];
            tween.Speed = speed;
            tweens[handle.ArrayIndex] = tween;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsActive(TweenHandle handle)
        {
            if (handle.Index < 0 || handle.Index >= nextTweenHandleIndex)
            {
                return false;
            }

            return handle.IsValid(tweenHandles[handle.Index]);
        }

        public int AddTransform(Transform transform)
        {
            int transformId = transform.GetInstanceID();
            if (!transformMap.ContainsKey(transformId))
            {
                transformMap[transformId] = transform;
            }
            return transformId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetActive(TweenHandle handle, bool active)
        {
            if (handle.Index < 0 || handle.Index >= nextTweenHandleIndex)
            {
                throw new InvalidOperationException("Invalid tween handle.");
            }

            if (!handle.IsValid(tweenHandles[handle.Index]))
            {
                throw new InvalidOperationException("Invalid tween handle.");
            }

            var tweenHandle = tweenHandles[handle.Index];
            tweenHandles[handle.Index] = new TweenHandle(
                    tweenHandle.Id,
                    tweenHandle.Index,
                    tweenHandle.ArrayIndex,
                    tweenHandle.Generation,
                    tweenHandle.Type,
                    active);
        }

        public void Dispatch(float deltaTime)
        {
            for (int handleIndex = 0; handleIndex < nextTweenHandleIndex; handleIndex++)
            {
                if (!tweenHandles[handleIndex].IsActive)
                {
                    continue;
                } 

                int tweenIndex = tweenHandles[handleIndex].ArrayIndex;
                var tween = tweens[tweenIndex];
                tween.IncreaseTime(deltaTime);
                tweens[tweenIndex] = tween;

                var value = tween.GetValue(tween.CurrentTime, lerpFunction);

                if (tween.Target.targetType == TweenTarget.TargetType.Transform)
                {
                    var transform = transformMap[tween.Target.targetObjectId];
                    if (tween.Target.propertyType == TweenTarget.PropertyType.Position)
                    {
                        transform.position = Unsafe.As<T, float3>(ref value);
                    }
                    else if (tween.Target.propertyType == TweenTarget.PropertyType.Rotation)
                    {
                    }
                    else if (tween.Target.propertyType == TweenTarget.PropertyType.Scale)
                    {
                        transform.localScale = Unsafe.As<T, float3>(ref value);
                    }
                }

                if (tween.IsComplete())
                {
                    RemoveTween(tweenHandles[handleIndex]);
                }
            }
        }
    }
}
