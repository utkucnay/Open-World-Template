using System.Collections.Generic;
using Glai.Collection;
using Glai.Core;
using Glai.ECS;
using Glai.Gameplay.Core;
using Glai.Module;
using Unity.Profiling;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Glai.Gameplay
{
    [Preserve]
    internal static class GameplayManagerModuleRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Register()
        {
            Glai.Module.RuntimeModuleCatalog.Register<GameplayManager>();
        }
    }

    [Preserve]
    public class GameplayManager : ModuleBase, IStart, ITick, ILateTick
    {
        public FixedList<int> archetypeIds;
        public List<System> systems;

        GameplayMemoryState gameplayMemoryState;

        Entity playerEntity;

        public override void Initialize()
        {
            gameplayMemoryState = new GameplayMemoryState();

            archetypeIds = new FixedList<int>(2, gameplayMemoryState.persistHandle, gameplayMemoryState);

            archetypeIds.Add(ECSAPI.CreateArchetype(stackalloc ArchetypeType[]
            {
                ArchetypeType.Component<PackedTransformComponent>(),
                ArchetypeType.Component<MeshRendererComponent>()
            }));

            archetypeIds.Add(ECSAPI.CreateArchetype(stackalloc ArchetypeType[]
            {
                ArchetypeType.Component<TransformComponent>()
            }));

            for (int i = 0; i < 100_000; i++)
            {
                var entity = ECSAPI.CreateEntity(archetypeIds[0]);
                ref var transform = ref ECSAPI.GetComponentRef<PackedTransformComponent>(entity);
                transform.position = new float3(i / 1000 * 2, 0f, i % 1000 * 2);
                transform.rotation = quaternion.identity;
            }

            playerEntity = ECSAPI.CreateEntity(archetypeIds[1]);

            systems = new List<System>()
            {
                new PlayerSystem(playerEntity),
                new TurnSystem(),
                new MeshRendererSystem()
            };
        }

        public void Start()
        {
            foreach (var system in systems)
            {
                system.Start();
            }
        }

        public override void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            if (systems != null)
            {
                foreach (var system in systems)
                {
                    system.Dispose();
                }

                systems.Clear();
                systems = null;
            }

            if (gameplayMemoryState != null)
            {
                archetypeIds.Dispose(gameplayMemoryState);
                gameplayMemoryState.Dispose();
            }

            base.Dispose();
        }

        public void Tick(float deltaTime)
        {
            foreach (var system in systems)
            {
                system.Tick(deltaTime);
            }
        }

        public void LateTick(float deltaTime)
        {
            foreach (var system in systems)
            {
                system.LateTick(deltaTime);
            }
        }
    }
}
