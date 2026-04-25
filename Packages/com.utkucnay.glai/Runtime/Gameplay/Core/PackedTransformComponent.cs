using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Glai.ECS;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Mathematics;
using static Unity.Burst.Intrinsics.X86;

namespace Glai.Gameplay
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PackedTransformComponent : IComponent
    {
        const float Snorm16PackScale = 32766f;

        public float3 position;
        public uint2 packedQuaternion;

        public quaternion rotation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => UnpackQuaternion(packedQuaternion);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => packedQuaternion = PackQuaternion(value.value);
        }

        public float3 forward => math.mul(rotation, new float3(0f, 0f, 1f));

        public float3 up => math.mul(rotation, new float3(0f, 1f, 0f));

        public float3 right => math.mul(rotation, new float3(1f, 0f, 0f));

        [MethodImpl(MethodImplOptions.AggressiveInlining), BurstCompile]
        public static uint2 PackQuaternion(float4 rotation)
        {
            float4 value = math.normalizesafe(rotation, new float4(0f, 0f, 0f, 1f));

            float4 mask = math.select(1f, -1f, value.w < 0f);
            value *= mask;

            return new uint2(
                PackSnorm16(value.x) | (PackSnorm16(value.y) << 16),
                PackSnorm16(value.z) | (PackSnorm16(value.w) << 16));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), BurstCompile]
        public static uint2 PackQuaternionYAxis(float2 value)
        {
            return new uint2(PackSnorm16(value.x) << 16, PackSnorm16(value.y) << 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), BurstCompile]
        public static unsafe void PackQuaternionYAxisAVX(float* sinHalf, float* cosHalf, uint2* result)
        {
            PackQuaternionYAxisAVX(Avx.mm256_loadu_ps(sinHalf), Avx.mm256_loadu_ps(cosHalf), result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), BurstCompile]
        public static unsafe void PackQuaternionYAxisAVX(v256 sinHalf, v256 cosHalf, uint2* result)
        {
            v256 min = Avx.mm256_set1_ps(-1f);
            v256 max = Avx.mm256_set1_ps(1f);
            v256 scale = Avx.mm256_set1_ps(Snorm16PackScale);

            v256 sinI32 = Avx.mm256_cvttps_epi32(Avx.mm256_mul_ps(Avx.mm256_min_ps(Avx.mm256_max_ps(sinHalf, min), max), scale));
            v256 cosI32 = Avx.mm256_cvttps_epi32(Avx.mm256_mul_ps(Avx.mm256_min_ps(Avx.mm256_max_ps(cosHalf, min), max), scale));

            v256 packed = Avx2.mm256_packs_epi32(sinI32, cosI32);

            result[0] = new uint2((uint)(ushort)packed.SShort0 << 16, (uint)(ushort)packed.SShort4 << 16);
            result[1] = new uint2((uint)(ushort)packed.SShort1 << 16, (uint)(ushort)packed.SShort5 << 16);
            result[2] = new uint2((uint)(ushort)packed.SShort2 << 16, (uint)(ushort)packed.SShort6 << 16);
            result[3] = new uint2((uint)(ushort)packed.SShort3 << 16, (uint)(ushort)packed.SShort7 << 16);
            result[4] = new uint2((uint)(ushort)packed.SShort8 << 16, (uint)(ushort)packed.SShort12 << 16);
            result[5] = new uint2((uint)(ushort)packed.SShort9 << 16, (uint)(ushort)packed.SShort13 << 16);
            result[6] = new uint2((uint)(ushort)packed.SShort10 << 16, (uint)(ushort)packed.SShort14 << 16);
            result[7] = new uint2((uint)(ushort)packed.SShort11 << 16, (uint)(ushort)packed.SShort15 << 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), BurstCompile]
        public static quaternion UnpackQuaternion(uint2 packedRotation)
        {
            float4 value = new float4(
                UnpackSnorm16(packedRotation.x),
                UnpackSnorm16(packedRotation.x >> 16),
                UnpackSnorm16(packedRotation.y),
                UnpackSnorm16(packedRotation.y >> 16));

            return math.normalizesafe(new quaternion(value), quaternion.identity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), BurstCompile]
        static uint PackSnorm16(float value)
        {
            int quantized = (int)(math.clamp(value, -1f, 1f) * Snorm16PackScale);
            return (uint)(ushort)(short)quantized;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), BurstCompile]
        static float UnpackSnorm16(uint packed)
        {
            return (short)(packed & 0xFFFFu) / 32767f;
        }
    }
}
