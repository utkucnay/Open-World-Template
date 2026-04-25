using System;

namespace Glai.Gameplay
{
    [Serializable]
    public struct TurnSystemConfig
    {
        public float PhaseMultiplier;
        public float VerticalAmplitude;
        public float RotationHalfAngleMultiplier;

        public static TurnSystemConfig Default => new TurnSystemConfig
        {
            PhaseMultiplier = 0.173f,
            VerticalAmplitude = 2f,
            RotationHalfAngleMultiplier = 0.5f,
        };
    }
}
