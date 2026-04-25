using UnityEngine;

namespace Glai.ECS
{
    [CreateAssetMenu(menuName = "Glai/ECS Config", fileName = "ECSConfig")]
    public class ECSConfigAsset : ScriptableObject
    {
        public EntityManagerConfig EntityManager = EntityManagerConfig.Default;
    }
}
