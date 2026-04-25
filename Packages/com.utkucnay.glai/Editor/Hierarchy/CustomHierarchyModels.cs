using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Glai.Core.Editor
{
    internal readonly struct SearchQuery
    {
        public readonly string NameContains;
        public readonly string TypeContains;
        public readonly string TagEquals;
        public readonly int? LayerEquals;

        public SearchQuery(string nameContains, string typeContains, string tagEquals, int? layerEquals)
        {
            NameContains = nameContains;
            TypeContains = typeContains;
            TagEquals = tagEquals;
            LayerEquals = layerEquals;
        }

        public bool HasStructuredTerms =>
            !string.IsNullOrEmpty(TypeContains) ||
            !string.IsNullOrEmpty(TagEquals) ||
            LayerEquals.HasValue;
    }

    internal readonly struct ContextTarget
    {
        public readonly GameObject Parent;
        public readonly Scene Scene;

        public ContextTarget(GameObject parent, Scene scene)
        {
            Parent = parent;
            Scene = scene;
        }

        public bool HasParent => Parent != null;
        public bool HasScene => Scene.IsValid() && Scene.isLoaded;
    }
}
