using Glai.ECS;
using Unity.Profiling;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using System.Runtime.CompilerServices;

namespace Glai.Gameplay
{
    [QueryJob(QueryExecution.ChunkParallel), BurstCompile]
    public struct TurnSystemQueryJob
    {
        public float time;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Execute(int entityId, Ref<PackedTransformComponent> transformComponent)
        {
            float phase = entityId * 0.173f;
            float t = time + phase;

            transformComponent.Value.position.y = math.sin(t) * 2f;

            float halfAngle = 0.5f * t;
            float sinHalf = math.sin(halfAngle);
            float cosHalf = math.cos(halfAngle);
            float4 rotation = new float4(0f, sinHalf, 0f, cosHalf);
            transformComponent.Value.packedQuaternion = PackedTransformComponent.PackQuaternion(rotation);
        }
    }

    [BurstCompile]
    public static class AAAAAA
    {
        [BurstCompile]
        public unsafe static void Execute(int entityId, PackedTransformComponent* transformComponent)
        {
            for (int i = 0; i < 1000; i++)
            {
                float phase = entityId * 0.173f;
                float t = 2 + phase;

                transformComponent[i].position.y = math.sin(t) * 2f;

                float halfAngle = 0.5f * t;
                float sinHalf = math.sin(halfAngle);
                float cosHalf = math.cos(halfAngle);
                float4 rotation = new float4(0f, sinHalf, 0f, cosHalf);
                transformComponent[i].packedQuaternion = PackedTransformComponent.PackQuaternion(rotation);
            }
        }
    }

    public class TurnSystem : System
    {
        static readonly ProfilerMarker s_TickPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "TurnSystem.Tick");

        public override void Start()
        {
        }

        public override void Tick(float deltaTime)
        {
            using (s_TickPerfMarker.Auto())
            {
                var job = new TurnSystemQueryJob()
                {
                    time = Time.time
                };

                var query = ECSAPI.Query().WithAll<PackedTransformComponent>();
                var handle = ECSAPI.Run(query, ref job);
                query.Dispose();

                handle.Complete();
                handle.Dispose();
            }
        }

        public override void LateTick(float deltaTime)
        {   
        }
    }
}
