using Glai.ECS;
using Glai.Module;
using Unity.Mathematics;
using UnityEngine;

namespace Glai.Gameplay
{
    public sealed class SampleGameplayBootstrap : MonoBehaviour
    {
        void Awake()
        {
            if (ModuleManager.Instance == null)
            {
                Debug.LogError("[SampleGameplayBootstrap] ModuleManager is not initialized.");
                return;
            }

            var gameplay = ModuleManager.Instance.GetModule<GameplayManager>();
            var config = gameplay.Config;

            int renderArchetype = gameplay.AddArchetype(ECSAPI.CreateArchetype(stackalloc ArchetypeType[]
            {
                ArchetypeType.Component<PackedTransformComponent>(),
                ArchetypeType.Component<MeshRendererComponent>()
            }));

            int playerArchetype = gameplay.AddArchetype(ECSAPI.CreateArchetype(stackalloc ArchetypeType[]
            {
                ArchetypeType.Component<TransformComponent>()
            }));

            for (int i = 0; i < config.RenderEntityCount; i++)
            {
                var entity = ECSAPI.CreateEntity(renderArchetype);
                ref var transform = ref ECSAPI.GetComponentRef<PackedTransformComponent>(entity);
                transform.position = new float3(
                    i / config.RenderGridWidth * config.RenderGridSpacing,
                    0f,
                    i % config.RenderGridWidth * config.RenderGridSpacing);
                transform.rotation = quaternion.identity;
            }

            var playerEntity = ECSAPI.CreateEntity(playerArchetype);
            gameplay.AddSystem(new PlayerSystem(playerEntity, config.Player));
            gameplay.AddSystem(new TurnSystem(config.Turn));
            gameplay.AddSystem(new MeshRendererSystem(config.MeshRenderer));

            Debug.Log($"[SampleGameplayBootstrap] Spawned {config.RenderEntityCount} render entities.");
        }
    }
}
