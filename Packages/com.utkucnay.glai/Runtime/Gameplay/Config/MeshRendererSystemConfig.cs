using System;

namespace Glai.Gameplay
{
    [Serializable]
    public struct MeshRendererSystemConfig
    {
        public int CubesPerCluster;
        public string MeshResourcePath;
        public string MaterialResourcePath;
        public string YAxisRotationKeyword;
        public string PackedTransformBufferProperty;
        public string ClusterSizeProperty;
        public float WorldBoundsSize;
        public int BoundsUpdateIntervalFrames;
        public float DummyHiddenY;
        public int InitialCapacity;
        public bool CastShadows;
        public bool ReceiveShadows;

        public static MeshRendererSystemConfig Default => new MeshRendererSystemConfig
        {
            CubesPerCluster = 1024 * 3,
            MeshResourcePath = "Cube.fbx",
            MaterialResourcePath = "Material/IndirectDefault",
            YAxisRotationKeyword = "_GLAI_Y_AXIS_ROTATION",
            PackedTransformBufferProperty = "_PackedTransformBuffer",
            ClusterSizeProperty = "_ClusterSize",
            WorldBoundsSize = 1_000_000f,
            BoundsUpdateIntervalFrames = 0,
            DummyHiddenY = -100000f,
            InitialCapacity = 1,
            CastShadows = true,
            ReceiveShadows = true,
        };
    }
}
