using System.Collections.Generic;
using Glai.Collection;
using Glai.Core;
using Glai.Gameplay.Core;
using Glai.Module;
using UnityEngine;
using UnityEngine.Scripting;

namespace Glai.Gameplay
{
    [Preserve, ModuleRegister(priority: 100)]
    public class GameplayManager : ModuleBase, IStart, ITick, ILateTick
    {
        const string ConfigResourcePath = "Glai/GameplayConfig";

        public GameplayManagerConfig Config { get; set; } = GameplayManagerConfig.Default;

        public FixedList<int> archetypeIds;
        public List<System> systems;

        GameplayMemoryState gameplayMemoryState;
        bool started;

        public override void Initialize()
        {
            LoadConfig();
            gameplayMemoryState = new GameplayMemoryState(Config.Memory);

            archetypeIds = new FixedList<int>(Config.ArchetypeListCapacity, gameplayMemoryState.persistHandle, gameplayMemoryState);
            systems = new List<System>();
        }

        private void LoadConfig()
        {
            var asset = Resources.Load<GameplayConfigAsset>(ConfigResourcePath);
            Config = asset != null ? asset.GameplayManager : GameplayManagerConfig.Default;
        }

        public void Start()
        {
            started = true;

            foreach (var system in systems)
            {
                system.Start();
            }
        }

        public int AddArchetype(int archetypeId)
        {
            archetypeIds.Add(archetypeId);
            return archetypeId;
        }

        public void AddSystem(System system)
        {
            systems.Add(system);

            if (started)
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

            started = false;

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
