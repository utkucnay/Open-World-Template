using UnityEngine;

namespace Glai.Gameplay
{
    [CreateAssetMenu(menuName = "Glai/Gameplay Config", fileName = "GameplayConfig")]
    public class GameplayConfigAsset : ScriptableObject
    {
        public GameplayManagerConfig GameplayManager = GameplayManagerConfig.Default;
    }
}
