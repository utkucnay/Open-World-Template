using Glai.ECS;
using Unity.Mathematics;
using UnityEngine;

namespace Glai.Gameplay
{
    public struct TransformComponent : IComponent
    {
        public float4x4 transform;

        public float3 position
        {
            get => transform.c3.xyz;
            set => transform.c3 = new float4(value, 1f);
        }

        public float3 scale
        {
            get => new float3(math.length(transform.c0.xyz), math.length(transform.c1.xyz), math.length(transform.c2.xyz));
            set
            {
                float3 currentScale = new float3(math.length(transform.c0.xyz), math.length(transform.c1.xyz), math.length(transform.c2.xyz));
                if (currentScale.x > 0f)
                {
                    transform.c0 = transform.c0 / currentScale.x * value.x;
                }
                if (currentScale.y > 0f)
                {
                    transform.c1 = transform.c1 / currentScale.y * value.y;
                }
                if (currentScale.z > 0f)
                {
                    transform.c2 = transform.c2 / currentScale.z * value.z;
                }
            }
        }
    }
}