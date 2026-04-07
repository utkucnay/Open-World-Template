using System;
using Glai.Core;
using Glai.Module;
using Glai.Tween.Core;
using Unity.Mathematics;
using UnityEngine;

namespace Glai.Tween 
{   
    internal struct TweenDispatch
    {
        public TweenDispatcher<float3> positionDispatcher;
        public TweenDispatcher<quaternion> rotationDispatcher;
        public TweenDispatcher<float3> scaleDispatcher;
        public TweenDispatcher<float> canvasAlphaDispatcher;

        internal void Dispatch(float deltaTime)
        {
            positionDispatcher.Dispatch(deltaTime);
            //rotationDispatcher.Dispatch(deltaTime);
            //scaleDispatcher.Dispatch(deltaTime);
        }
    }

    [ModuleRegister]
    internal class TweenManager : ModuleBase, ITick, ITweenManager
    {
        TweenDispatch tweenDispatch;
        
        SequenceDispatcher sequenceDispatcher;

        TweenState tweenState;

        public float GlobalSpeed { get; set; }

        public static TweenManager Instance { get; private set; }

        public TweenState TweenState => tweenState;
        
        public override void Initialize()
        {
            Instance = this;
            ITweenManager.Instance = this;

            tweenState = new TweenState(default);
            tweenDispatch.positionDispatcher = new TweenDispatcher<float3>(
                TweenType.Position, math.lerp);
            
            sequenceDispatcher = new SequenceDispatcher(100, tweenState.tweenPersistHandle, tweenState);
            GlobalSpeed = 1f;
        }

        public void SetTweenSpeed(TweenHandle handle, float speed)
        {
            switch (handle.Type)
            {
                case TweenType.Position:
                    tweenDispatch.positionDispatcher.SetTweenSpeed(handle, speed);
                    break;
                case TweenType.Rotation:
                case TweenType.Scale:
                    throw new NotImplementedException($"TweenType.{handle.Type} is not yet supported.");
            }
        }

        public bool IsTweenActive(TweenHandle handle)
        {
            switch (handle.Type)
            {
                case TweenType.Position:
                    return tweenDispatch.positionDispatcher.IsActive(handle);
                case TweenType.Rotation:
                case TweenType.Scale:
                    throw new NotImplementedException($"TweenType.{handle.Type} is not yet supported.");
                default:
                    return false;
            }
        }

        public bool SetTweenActive(TweenHandle handle, bool isActive)
        {
            switch (handle.Type)
            {
                case TweenType.Position:
                    tweenDispatch.positionDispatcher.SetActive(handle, isActive);
                    break;
                case TweenType.Rotation:
                case TweenType.Scale:
                    throw new NotImplementedException($"TweenType.{handle.Type} is not yet supported.");
                default:
                    return false;
            }
            return true;
        }


        public void Tick(float deltaTime)
        {
            DispatchTweens();
        }

        public TweenHandle AddPositionTween(float3 from, float3 to, float duration, Transform transform)
        {
            int transformId = tweenDispatch.positionDispatcher.AddTransform(transform);
            var target = new TweenTarget(transformId, TweenTarget.TargetType.Transform, TweenTarget.PropertyType.Position);
            return tweenDispatch.positionDispatcher.AddTween(from, to, duration, target, "PositionTween");
        }

        public void AddSequence(in SequenceBuilder sequenceBuilder)
        {
            var sequence = new Sequence(sequenceBuilder.tweens.Count, sequenceBuilder.allocatorHandle, sequenceBuilder.memoryState);

            for (int i = 0; i < sequenceBuilder.tweens.Count; i++)
            {
                var concurrentTweens = sequenceBuilder.tweens.Get(i);
                sequence.Add(concurrentTweens.AsSpan(), sequenceBuilder.memoryState);
            }

            sequenceDispatcher.AddSequence(sequence, ref tweenDispatch);
        }

        public void DispatchTweens()
        {
            sequenceDispatcher.BeforeDispatch(ref tweenDispatch);
            tweenDispatch.Dispatch(Time.deltaTime * GlobalSpeed);
        }
    }
}
