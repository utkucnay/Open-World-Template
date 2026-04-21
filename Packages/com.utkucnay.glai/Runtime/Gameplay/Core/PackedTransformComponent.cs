using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Glai.ECS;
using Unity.Burst;
using Unity.Mathematics;

namespace Glai.Gameplay
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PackedTransformComponent : IComponent
    {
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

            if (value.w < 0f)
                value = -value;

            return new uint2(
                PackSnorm16(value.x) | (PackSnorm16(value.y) << 16),
                PackSnorm16(value.z) | (PackSnorm16(value.w) << 16));
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
            int quantized = (int)math.round(math.clamp(value, -1f, 1f) * 32767f);
            return (uint)(ushort)(short)quantized;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), BurstCompile]
        static float UnpackSnorm16(uint packed)
        {
            return (short)(packed & 0xFFFFu) / 32767f;
        }
    }
}
