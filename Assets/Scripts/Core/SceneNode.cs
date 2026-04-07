using UnityEngine;
using Unity.Mathematics;

namespace Glai.Core
{
    public abstract class SceneNode : Object
    {
        public GameObject UnityObject { get; private set; }
        public Transform UnityTransform { get; private set; }

        public SceneNode()
        {
            UnityObject = new GameObject();
            UnityTransform = UnityObject.transform;
        }

        public SceneNode(Transform parent)
        {
            UnityObject = new GameObject();
            UnityTransform = UnityObject.transform;
            UnityTransform.parent = parent;
        }

        ~SceneNode()
        {
        }

        public float4x4 Transform { get => UnityTransform.localToWorldMatrix; }
        public float3 Position { get => UnityTransform.position; set => UnityTransform.position = value; }
        public float3 LocalPosition { get => UnityTransform.localPosition; set => UnityTransform.localPosition = value; }
        public quaternion Rotation { get => UnityTransform.rotation; set => UnityTransform.rotation = value; }
        public quaternion LocalRotation { get => UnityTransform.localRotation; set => UnityTransform.localRotation = value; }
        public float3 LocalScale { get => UnityTransform.localScale; set => UnityTransform.localScale = value; }
    }    
}