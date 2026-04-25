using Glai.ECS;
using Unity.Profiling;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using static Unity.Burst.Intrinsics.X86;


namespace Glai.Gameplay
{
    [QueryJob(QueryExecution.ChunkParallel), BurstCompile]
    public unsafe struct TurnSystemQueryJob
    {
        public float time;
        public TurnSystemConfig config;

        public void ExecuteAVX(EntityId8 entityIds, RefRW8<PackedTransformComponent> transformComponents)
        {
            uint2* packedRotation = stackalloc uint2[EntityId8.Length];

            v256 ids = Avx.mm256_cvtepi32_ps(Avx.mm256_loadu_si256(entityIds.Ptr));
            v256 t = Avx.mm256_add_ps(
                Avx.mm256_mul_ps(ids, Avx.mm256_set1_ps(config.PhaseMultiplier)),
                Avx.mm256_set1_ps(time));

            v256 positionY = Avx.mm256_mul_ps(SinAVX(t), Avx.mm256_set1_ps(config.VerticalAmplitude));
            v256 halfAngle = Avx.mm256_mul_ps(t, Avx.mm256_set1_ps(config.RotationHalfAngleMultiplier));
            SinCosAVX(halfAngle, out v256 sinHalf, out v256 cosHalf);

            PackedTransformComponent.PackQuaternionYAxisAVX(sinHalf, cosHalf, packedRotation);

            transformComponents[0].position.y = positionY.Float0;
            transformComponents[1].position.y = positionY.Float1;
            transformComponents[2].position.y = positionY.Float2;
            transformComponents[3].position.y = positionY.Float3;
            transformComponents[4].position.y = positionY.Float4;
            transformComponents[5].position.y = positionY.Float5;
            transformComponents[6].position.y = positionY.Float6;
            transformComponents[7].position.y = positionY.Float7;

            for (int i = 0; i < EntityId8.Length; i++)
                transformComponents[i].packedQuaternion = packedRotation[i];
        }

        static void SinCosAVX(v256 value, out v256 sin, out v256 cos)
        {
            sin = SinAVX(value);
            cos = SinAVX(Avx.mm256_add_ps(value, Avx.mm256_set1_ps(1.5707963267948966f)));
        }

        static v256 SinAVX(v256 value)
        {
            const float twoPi = 6.283185307179586f;
            const float inverseTwoPi = 0.15915494309189535f;
            const float pi = 3.141592653589793f;
            const float fivePiSquared = 49.34802200544679f;

            v256 nearestTurns = Avx.mm256_round_ps(
                Avx.mm256_mul_ps(value, Avx.mm256_set1_ps(inverseTwoPi)),
                (int)RoundingMode.FROUND_NINT_NOEXC);
            v256 x = Avx.mm256_sub_ps(value, Avx.mm256_mul_ps(nearestTurns, Avx.mm256_set1_ps(twoPi)));
            v256 absX = Avx.mm256_andnot_ps(Avx.mm256_set1_epi32(unchecked((int)0x80000000)), x);
            v256 piMinusAbsX = Avx.mm256_sub_ps(Avx.mm256_set1_ps(pi), absX);
            v256 numerator = Avx.mm256_mul_ps(Avx.mm256_set1_ps(16f), Avx.mm256_mul_ps(x, piMinusAbsX));
            v256 denominator = Avx.mm256_sub_ps(
                Avx.mm256_set1_ps(fivePiSquared),
                Avx.mm256_mul_ps(Avx.mm256_set1_ps(4f), Avx.mm256_mul_ps(absX, piMinusAbsX)));
            return Avx.mm256_div_ps(numerator, denominator);
        }
    }

    public class TurnSystem : System
    {
        static readonly ProfilerMarker s_TickPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "TurnSystem.Tick");
        TurnSystemConfig config;

        public TurnSystem() : this(TurnSystemConfig.Default)
        {
        }

        public TurnSystem(TurnSystemConfig config)
        {
            this.config = config;
        }

        public override void Start()
        {
        }

        public override void Tick(float deltaTime)
        {
            using (s_TickPerfMarker.Auto())
            {
                var job = new TurnSystemQueryJob()
                {
                    time = Time.time,
                    config = config
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
