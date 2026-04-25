using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Glai.Core.Editor
{
    internal static class CustomHierarchyMenus
    {
        private static readonly string[] CreateCommands =
        {
            "GameObject/Create Empty",
            "GameObject/Create Empty Child",
            "GameObject/3D Object/Cube",
            "GameObject/3D Object/Sphere",
            "GameObject/3D Object/Capsule",
            "GameObject/3D Object/Cylinder",
            "GameObject/3D Object/Plane",
            "GameObject/3D Object/Quad",
            "GameObject/3D Object/Text - TextMeshPro",
            "GameObject/Light/Directional Light",
            "GameObject/Light/Point Light",
            "GameObject/Light/Spot Light",
            "GameObject/Light/Area Light",
            "GameObject/Audio/Audio Source",
            "GameObject/Effects/Particle System",
            "GameObject/Video/Video Player",
            "GameObject/UI/Canvas",
            "GameObject/UI/Text - TextMeshPro",
            "GameObject/Camera",
            "GameObject/XR/XR Origin (VR)",
            "GameObject/XR/XR Origin (AR)",
        };

        public static void BuildCreateMenu(GenericMenu menu, ContextTarget target, bool includeCreateEmptyChild, Action<string, ContextTarget> executeCreateCommand, Action<ContextTarget> createEmptyParentForSelection)
        {
            for (int i = 0; i < CreateCommands.Length; i++)
            {
                string command = CreateCommands[i];
                if (!includeCreateEmptyChild && string.Equals(command, "GameObject/Create Empty Child", StringComparison.Ordinal))
                {
                    continue;
                }

                string displayPath = command.Substring("GameObject/".Length);
                menu.AddItem(new GUIContent($"Create/{displayPath}"), false, () => executeCreateCommand(command, target));
            }

            menu.AddSeparator("Create/");
            menu.AddItem(new GUIContent("Create/Empty Parent"), false, () => createEmptyParentForSelection(target));
        }

        public static void AddObjectActions(
            GenericMenu menu,
            GameObject target,
            bool hasPasteBuffer,
            Action<GameObject> renameObject,
            Action duplicateSelection,
            Action deleteSelection,
            Action<GameObject> createEmptyParent,
            Action<GameObject, int> setSiblingIndex,
            Action moveSelectionToView,
            Action<GameObject> unpackPrefab,
            Action copySelection,
            Action<GameObject, Scene> pasteSelection,
            Action<GameObject, bool> setActive)
        {
            menu.AddItem(new GUIContent("Rename"), false, () => renameObject(target));
            menu.AddItem(new GUIContent("Duplicate"), false, () => duplicateSelection());
            menu.AddItem(new GUIContent("Delete"), false, () => deleteSelection());

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Create Empty Parent"), false, () => createEmptyParent(target));

            if (target.transform.parent != null)
            {
                menu.AddItem(new GUIContent("Set As First Sibling"), false, () => setSiblingIndex(target, 0));
                menu.AddItem(new GUIContent("Set As Last Sibling"), false, () => setSiblingIndex(target, -1));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Set As First Sibling"));
                menu.AddDisabledItem(new GUIContent("Set As Last Sibling"));
            }

            menu.AddItem(new GUIContent("Move To View"), false, () => moveSelectionToView());

            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                menu.AddItem(new GUIContent("Unpack Prefab Completely"), false, () => unpackPrefab(target));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Unpack Prefab Completely"));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Copy"), false, () => copySelection());
            if (hasPasteBuffer)
            {
                menu.AddItem(new GUIContent("Paste"), false, () => pasteSelection(target, target.scene));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent(target.activeSelf ? "Set Inactive" : "Set Active"), false, () => setActive(target, !target.activeSelf));

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Ping"), false, () => EditorGUIUtility.PingObject(target));
            menu.AddItem(new GUIContent("Frame Selected"), false, () => CustomHierarchyActions.FrameObject(target));
        }

        public static void AddGeneralEditActions(
            GenericMenu menu,
            bool hasPasteBuffer,
            Action renameSelection,
            Action duplicateSelection,
            Action deleteSelection,
            Action copySelection,
            Action pasteSelection,
            Action createEmptyParent,
            Action moveSelectionToView)
        {
            bool hasSelection = Selection.gameObjects.Length > 0;
            if (hasSelection)
            {
                menu.AddItem(new GUIContent("Rename"), false, () => renameSelection());
                menu.AddItem(new GUIContent("Duplicate"), false, () => duplicateSelection());
                menu.AddItem(new GUIContent("Delete"), false, () => deleteSelection());
                menu.AddItem(new GUIContent("Copy"), false, () => copySelection());
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Rename"));
                menu.AddDisabledItem(new GUIContent("Duplicate"));
                menu.AddDisabledItem(new GUIContent("Delete"));
                menu.AddDisabledItem(new GUIContent("Copy"));
            }

            if (hasPasteBuffer)
            {
                menu.AddItem(new GUIContent("Paste"), false, () => pasteSelection());
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }

            menu.AddSeparator(string.Empty);
            if (Selection.activeGameObject != null)
            {
                menu.AddItem(new GUIContent("Create Empty Parent"), false, () => createEmptyParent());
                menu.AddItem(new GUIContent("Move To View"), false, () => moveSelectionToView());
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Create Empty Parent"));
                menu.AddDisabledItem(new GUIContent("Move To View"));
            }
        }
    }
}
