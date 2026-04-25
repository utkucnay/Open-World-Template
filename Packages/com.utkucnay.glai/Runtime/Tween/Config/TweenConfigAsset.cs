using UnityEngine;

namespace Glai.Tween
{
    [CreateAssetMenu(menuName = "Glai/Tween Config", fileName = "TweenConfig")]
    public class TweenConfigAsset : ScriptableObject
    {
        public TweenManagerConfig TweenManager = TweenManagerConfig.Default;
    }
}
