using Unity.Collections;
using System;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;

namespace Glai.Tween.Core
{
    public struct Tween<T> where T : unmanaged
    {
        private FixedString128Bytes debugName;
        private T fromValue;
        private T toValue;
        private float duration;
        private TweenTarget target;
        private float speed;
        private float currentTime;

        public Tween(T fromValue, T toValue, float duration, TweenTarget target, FixedString128Bytes debugName = default)
        {
            this.fromValue = fromValue;
            this.toValue = toValue;
            this.duration = duration;
            this.target = target;
            this.debugName = debugName;
            this.speed = 1f;
            this.currentTime = 0f;
        }

        public TweenTarget Target => target;
        public float Duration => duration;
        public float CurrentTime => currentTime;
        public float Speed { get => speed; set => speed = value; }
        public FixedString128Bytes DebugName => debugName;

        public T FromValue => fromValue;
        public T ToValue => toValue;

        public T GetValue(float time, Func<T, T, float, T> lerpFunction)
        {
            if (duration <= 0f)
            {
                return toValue;
            }

            return lerpFunction(fromValue, toValue, time / duration);
        }

        public bool IsComplete()
        {
            return currentTime >= duration;
        }

        public void IncreaseTime(float deltaTime)
        {
            currentTime += deltaTime * speed;
            currentTime = math.min(currentTime, duration);
        }
    }
}
