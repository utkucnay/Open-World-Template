using System;
using Glai.Gameplay.Core;

namespace Glai.Gameplay
{
    [Serializable]
    public struct GameplayManagerConfig
    {
        public int ArchetypeListCapacity;
        public int RenderEntityCount;
        public int RenderGridWidth;
        public float RenderGridSpacing;
        public GameplayMemoryConfig Memory;
        public PlayerSystemConfig Player;
        public TurnSystemConfig Turn;
        public MeshRendererSystemConfig MeshRenderer;

        public static GameplayManagerConfig Default => new GameplayManagerConfig
        {
            ArchetypeListCapacity = 2,
            RenderEntityCount = 100_000,
            RenderGridWidth = 1000,
            RenderGridSpacing = 2f,
            Memory = GameplayMemoryConfig.Default,
            Player = PlayerSystemConfig.Default,
            Turn = TurnSystemConfig.Default,
            MeshRenderer = MeshRendererSystemConfig.Default,
        };
    }
}
