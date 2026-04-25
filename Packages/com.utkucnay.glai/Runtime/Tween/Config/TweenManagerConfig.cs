using System;
using Glai.Tween.Core;

namespace Glai.Tween
{
    [Serializable]
    public struct TweenManagerConfig
    {
        public TweenStateData State;
        public int DispatcherCapacity;
        public int SequenceCapacity;
        public float GlobalSpeed;

        public static TweenManagerConfig Default => new TweenManagerConfig
        {
            State = TweenStateData.Default,
            DispatcherCapacity = 256,
            SequenceCapacity = 100,
            GlobalSpeed = 1f,
        };
    }
}
