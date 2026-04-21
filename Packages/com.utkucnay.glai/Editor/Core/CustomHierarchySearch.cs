using System;
using System.Collections.Generic;
using UnityEngine;

namespace Glai.Core.Editor
{
    internal static class CustomHierarchySearch
    {
        public static void Apply(CustomHierarchyTreeView treeView, string rawSearch, bool reload)
        {
            if (treeView == null)
            {
                return;
            }

            SearchQuery query = Parse(rawSearch);
            treeView.searchString = query.NameContains;
            treeView.AdditionalFilter = query.HasStructuredTerms ? (Func<GameObject, bool>)(go => Matches(go, query)) : null;

            if (reload)
            {
                treeView.Reload();
            }
        }

        private static SearchQuery Parse(string rawSearch)
        {
            if (string.IsNullOrWhiteSpace(rawSearch))
            {
                return new SearchQuery(string.Empty, string.Empty, string.Empty, null);
            }

            string[] tokens = rawSearch.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> freeText = new List<string>();
            string typeContains = string.Empty;
            string tagEquals = string.Empty;
            int? layerEquals = null;

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                int separator = token.IndexOf(':');
                if (separator <= 0 || separator >= token.Length - 1)
                {
                    freeText.Add(token);
                    continue;
                }

                string key = token.Substring(0, separator).ToLowerInvariant();
                string value = token.Substring(separator + 1);

                if (key == "t" || key == "type")
                {
                    typeContains = value;
                    continue;
                }

                if (key == "tag")
                {
                    tagEquals = value;
                    continue;
                }

                if (key == "l" || key == "layer")
                {
                    if (int.TryParse(value, out int numericLayer))
                    {
                        layerEquals = numericLayer;
                    }
                    else
                    {
                        int namedLayer = LayerMask.NameToLayer(value);
                        if (namedLayer >= 0)
                        {
                            layerEquals = namedLayer;
                        }
                    }

                    continue;
                }

                if (key == "name")
                {
                    freeText.Add(value);
                    continue;
                }

                freeText.Add(token);
            }

            return new SearchQuery(string.Join(" ", freeText), typeContains, tagEquals, layerEquals);
        }

        private static bool Matches(GameObject gameObject, SearchQuery query)
        {
            if (!string.IsNullOrEmpty(query.TypeContains) && !HasComponentTypeLike(gameObject, query.TypeContains))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(query.TagEquals) && !string.Equals(gameObject.tag, query.TagEquals, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !query.LayerEquals.HasValue || gameObject.layer == query.LayerEquals.Value;
        }

        private static bool HasComponentTypeLike(GameObject gameObject, string typeContains)
        {
            Component[] components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                {
                    continue;
                }

                if (component.GetType().Name.IndexOf(typeContains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
