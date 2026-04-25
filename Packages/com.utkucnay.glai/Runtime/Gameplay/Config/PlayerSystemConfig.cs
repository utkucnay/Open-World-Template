using System;
using UnityEngine;

namespace Glai.Gameplay
{
    [Serializable]
    public struct PlayerSystemConfig
    {
        public float MoveSpeed;
        public float FastMoveMultiplier;
        public float LookSensitivity;
        public Vector3 DefaultCameraPosition;
        public Vector3 LookTarget;
        public float MinPitch;
        public float MaxPitch;

        public static PlayerSystemConfig Default => new PlayerSystemConfig
        {
            MoveSpeed = 10f,
            FastMoveMultiplier = 3f,
            LookSensitivity = 180f,
            DefaultCameraPosition = new Vector3(100f, 160f, -160f),
            LookTarget = new Vector3(100f, 0f, 900f),
            MinPitch = -89f,
            MaxPitch = 89f,
        };
    }
}
