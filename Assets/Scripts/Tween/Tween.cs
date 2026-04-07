using System.Runtime.CompilerServices;
using Glai.Core;
using Glai.Tween.Core;
using Unity.Mathematics;
using UnityEngine;

namespace Glai.Tween
{
    public static class Tween
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTweenSpeed(TweenHandle handle, float speed)
        {
            TweenManager.Instance.SetTweenSpeed(handle, speed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetGlobalSpeed(float speed)
        {
            TweenManager.Instance.GlobalSpeed = speed;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SequenceBuilder CreateSequence(int capacity, int concurrentTweenCapacity)
        {
            return new SequenceBuilder(capacity, concurrentTweenCapacity, TweenManager.Instance.TweenState.PopArenaHandle(), TweenManager.Instance.TweenState);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenHandle DoMove(this Transform transform, float3 from, float3 to, float t)
        {
            return TweenManager.Instance.AddPositionTween(from, to, t, transform);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenHandle DoMoveX(this Transform transform, float from, float to, float t)
        {
            return TweenManager.Instance.AddPositionTween(
                new float3(from, transform.position.y, transform.position.z), new float3(to, transform.position.y, transform.position.z), 
                t, transform);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenHandle DoMoveY(this Transform transform, float from, float to, float t)
        {
            return TweenManager.Instance.AddPositionTween(
                new float3(transform.position.x, from, transform.position.z), new float3(transform.position.x, to, transform.position.z), 
                t, transform);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenHandle DoMoveZ(this Transform transform, float from, float to, float t)
        {
            return TweenManager.Instance.AddPositionTween(
                new float3(transform.position.x, transform.position.y, from), new float3(transform.position.x, transform.position.y, to), 
                t, transform);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenHandle DoMove(this Transform transform, float3 to, float t)
        {
            return TweenManager.Instance.AddPositionTween(new float3(transform.position.x, transform.position.y, transform.position.z), to, t, transform);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenHandle DoMoveX(this Transform transform, float to, float t)
        {
            return TweenManager.Instance.AddPositionTween(
                new float3(transform.position.x, transform.position.y, transform.position.z), new float3(to, transform.position.y, transform.position.z), 
                t, transform);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenHandle DoMoveY(this Transform transform, float to, float t)
        {
            return TweenManager.Instance.AddPositionTween(
                new float3(transform.position.x, transform.position.y, transform.position.z), new float3(transform.position.x, to, transform.position.z), 
                t, transform);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TweenHandle DoMoveZ(this Transform transform, float to, float t)
        {
            return TweenManager.Instance.AddPositionTween(
                new float3(transform.position.x, transform.position.y, transform.position.z), new float3(transform.position.x, transform.position.y, to), 
                t, transform);
        }
    }
}
