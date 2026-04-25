using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Glai.Core.Editor
{
    internal static class CustomHierarchyStyles
    {
        private static GUIStyle sceneCountLabelStyle;
        private static GUIStyle prefabLabelStyle;
        private static GUIStyle inactiveLabelStyle;
        private static GUIStyle selectedPrefabLabelStyle;
        private static GUIStyle selectedInactiveLabelStyle;
        private static GUIStyle selectedLabelStyle;
        private static GUIStyle ecsLabelStyle;
        private static GUIStyle selectedEcsLabelStyle;

        private static GUIStyle SceneCountLabelStyle
        {
            get
            {
                if (sceneCountLabelStyle == null)
                {
                    sceneCountLabelStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                    {
                        alignment = TextAnchor.MiddleRight,
                    };
                }

                return sceneCountLabelStyle;
            }
        }

        private static GUIStyle PrefabLabelStyle => prefabLabelStyle ??= CreateLabelStyle(new Color(0.36f, 0.62f, 0.96f));
        private static GUIStyle InactiveLabelStyle => inactiveLabelStyle ??= CreateLabelStyle(new Color(0.62f, 0.62f, 0.62f));
        private static GUIStyle SelectedPrefabLabelStyle => selectedPrefabLabelStyle ??= CreateLabelStyle(Color.white);
        private static GUIStyle SelectedInactiveLabelStyle => selectedInactiveLabelStyle ??= CreateLabelStyle(new Color(0.92f, 0.92f, 0.92f));
        private static GUIStyle SelectedLabelStyle => selectedLabelStyle ??= CreateLabelStyle(Color.white);
        public static GUIStyle EcsLabelStyle => ecsLabelStyle ??= CreateLabelStyle(EditorStyles.label.normal.textColor);
        public static GUIStyle SelectedEcsLabelStyle => selectedEcsLabelStyle ??= CreateLabelStyle(Color.white);

        public static void DrawSceneHeader(Rect rowRect, HierarchyTreeItem item)
        {
            bool isActiveScene = item.Scene == SceneManager.GetActiveScene();

            if (Event.current.type == EventType.Repaint)
            {
                Color background = isActiveScene
                    ? new Color(0.19f, 0.35f, 0.53f, 0.28f)
                    : new Color(0.24f, 0.24f, 0.24f, 0.24f);
                EditorGUI.DrawRect(rowRect, background);
            }

            Rect labelRect = rowRect;
            labelRect.xMin += 18f;
            labelRect.xMax -= 72f;

            string sceneName = string.IsNullOrEmpty(item.Scene.name) ? "untitled" : item.Scene.name;
            string dirtyMarker = item.Scene.isDirty ? " *" : string.Empty;
            string activeMarker = isActiveScene ? " [Active]" : string.Empty;
            EditorGUI.LabelField(labelRect, $"{sceneName}{dirtyMarker}{activeMarker}", EditorStyles.boldLabel);

            Rect statusRect = rowRect;
            statusRect.xMin = rowRect.xMax - 68f;
            EditorGUI.LabelField(statusRect, item.VisibleObjectCount.ToString(), SceneCountLabelStyle);
        }

        public static GUIStyle GetObjectLabelStyle(GameObject gameObject, bool isSelected)
        {
            if (gameObject == null)
            {
                return EditorStyles.label;
            }

            if (!gameObject.activeInHierarchy)
            {
                return isSelected ? SelectedInactiveLabelStyle : InactiveLabelStyle;
            }

            if (PrefabUtility.IsPartOfAnyPrefab(gameObject))
            {
                return isSelected ? SelectedPrefabLabelStyle : PrefabLabelStyle;
            }

            return isSelected ? SelectedLabelStyle : EditorStyles.label;
        }

        private static GUIStyle CreateLabelStyle(Color color)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = color;
            return style;
        }
    }
}
