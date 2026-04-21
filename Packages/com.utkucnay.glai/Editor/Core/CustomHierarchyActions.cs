using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Glai.Core.Editor
{
    internal static class CustomHierarchyActions
    {
        public static bool HandleObjectsDropped(CustomHierarchyTreeView treeView, IReadOnlyList<GameObject> draggedObjects, int targetId, int insertAtIndex, bool performDrop, Action delayRefresh)
        {
            if (draggedObjects == null || draggedObjects.Count == 0)
            {
                return false;
            }

            GameObject targetParent = null;
            Scene targetScene = SceneManager.GetActiveScene();
            int siblingIndex = -1;

            if (targetId != 0 && treeView != null && treeView.TryGetHierarchyItem(targetId, out HierarchyTreeItem targetItem))
            {
                if (targetItem.IsSceneHeader)
                {
                    targetScene = targetItem.Scene;
                    siblingIndex = Mathf.Max(0, insertAtIndex);
                }
                else
                {
                    GameObject targetObject = targetItem.GameObject;
                    targetScene = targetObject.scene;

                    if (insertAtIndex < 0)
                    {
                        targetParent = targetObject;
                    }
                    else
                    {
                        Transform targetTransform = targetObject.transform;
                        Transform parentTransform = targetTransform.parent;
                        targetParent = parentTransform != null ? parentTransform.gameObject : null;
                        siblingIndex = targetTransform.GetSiblingIndex();
                        if (insertAtIndex > siblingIndex)
                        {
                            siblingIndex += 1;
                        }
                    }
                }
            }

            if (!targetScene.IsValid() || !targetScene.isLoaded)
            {
                return false;
            }

            for (int i = 0; i < draggedObjects.Count; i++)
            {
                GameObject dragged = draggedObjects[i];
                if (dragged == null)
                {
                    return false;
                }

                if (targetParent == null)
                {
                    continue;
                }

                if (dragged == targetParent || targetParent.transform.IsChildOf(dragged.transform))
                {
                    return false;
                }
            }

            if (!performDrop)
            {
                return true;
            }

            for (int i = 0; i < draggedObjects.Count; i++)
            {
                GameObject dragged = draggedObjects[i];
                Undo.SetTransformParent(dragged.transform, targetParent != null ? targetParent.transform : null, "Reparent GameObjects");

                if (siblingIndex >= 0)
                {
                    int resolvedIndex = Mathf.Min(siblingIndex, dragged.transform.parent != null
                        ? dragged.transform.parent.childCount - 1
                        : dragged.scene.rootCount - 1);
                    dragged.transform.SetSiblingIndex(Mathf.Max(0, resolvedIndex));
                    siblingIndex += 1;
                }

                if (dragged.scene != targetScene)
                {
                    SceneManager.MoveGameObjectToScene(dragged, targetScene);
                }

                if (dragged.scene.IsValid() && dragged.scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(dragged.scene);
                }
            }

            UnityEngine.Object[] selectedObjects = new UnityEngine.Object[draggedObjects.Count];
            for (int i = 0; i < draggedObjects.Count; i++)
            {
                selectedObjects[i] = draggedObjects[i];
            }

            Selection.objects = selectedObjects;
            delayRefresh?.Invoke();
            return true;
        }

        public static void ClearSelection(CustomHierarchyTreeView treeView)
        {
            treeView?.SetSelection(new List<int>());
            Selection.activeObject = null;
        }

        public static void RenameObject(GameObject target, CustomHierarchyTreeView treeView)
        {
            if (target == null || treeView == null)
            {
                return;
            }

            Selection.activeGameObject = target;
            treeView.SetSelection(new List<int> { target.GetInstanceID() }, TreeViewSelectionOptions.RevealAndFrame);
            treeView.BeginRenameById(target.GetInstanceID());
        }

        public static void DeleteSelection(CustomHierarchyTreeView treeView, Action delayRefresh)
        {
            List<GameObject> selected = GetTopLevelSelection(GetSelectedSceneObjects());
            if (selected.Count == 0)
            {
                return;
            }

            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i] != null)
                {
                    Undo.DestroyObjectImmediate(selected[i]);
                }
            }

            ClearSelection(treeView);
            delayRefresh?.Invoke();
        }

        public static void DuplicateSelection(Action delayRefresh)
        {
            List<GameObject> selected = GetTopLevelSelection(GetSelectedSceneObjects());
            if (selected.Count == 0)
            {
                return;
            }

            List<GameObject> duplicates = new List<GameObject>(selected.Count);
            for (int i = 0; i < selected.Count; i++)
            {
                GameObject source = selected[i];
                if (source == null)
                {
                    continue;
                }

                Transform parent = source.transform.parent;
                GameObject clone = UnityEngine.Object.Instantiate(source, parent);
                clone.name = source.name;
                clone.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

                if (source.scene.IsValid() && source.scene.isLoaded && clone.scene != source.scene)
                {
                    SceneManager.MoveGameObjectToScene(clone, source.scene);
                }

                Undo.RegisterCreatedObjectUndo(clone, "Duplicate GameObject");

                if (clone.scene.IsValid() && clone.scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(clone.scene);
                }

                duplicates.Add(clone);
            }

            if (duplicates.Count > 0)
            {
                Selection.objects = duplicates.ToArray();
                delayRefresh?.Invoke();
            }
        }

        public static void CopySelection(List<GameObject> copyBuffer)
        {
            copyBuffer.Clear();
            List<GameObject> selected = GetTopLevelSelection(GetSelectedSceneObjects());
            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i] != null)
                {
                    copyBuffer.Add(selected[i]);
                }
            }
        }

        public static void PasteSelection(List<GameObject> copyBuffer, GameObject preferredParent, Scene fallbackScene, Action delayRefresh)
        {
            if (copyBuffer.Count == 0)
            {
                return;
            }

            GameObject targetParent = preferredParent != null ? preferredParent : Selection.activeGameObject;
            Scene targetScene = targetParent != null
                ? targetParent.scene
                : (fallbackScene.IsValid() && fallbackScene.isLoaded ? fallbackScene : SceneManager.GetActiveScene());

            if (!targetScene.IsValid() || !targetScene.isLoaded)
            {
                return;
            }

            List<GameObject> pasted = new List<GameObject>();
            for (int i = 0; i < copyBuffer.Count; i++)
            {
                GameObject source = copyBuffer[i];
                if (source == null)
                {
                    continue;
                }

                Transform parentTransform = targetParent != null ? targetParent.transform : null;
                GameObject clone = UnityEngine.Object.Instantiate(source, parentTransform);
                clone.name = source.name;

                if (clone.scene != targetScene)
                {
                    SceneManager.MoveGameObjectToScene(clone, targetScene);
                }

                Undo.RegisterCreatedObjectUndo(clone, "Paste GameObject");

                if (clone.scene.IsValid() && clone.scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(clone.scene);
                }

                pasted.Add(clone);
            }

            if (pasted.Count > 0)
            {
                Selection.objects = pasted.ToArray();
                delayRefresh?.Invoke();
            }
        }

        public static void CreateEmptyParentForSelection(ContextTarget target, Action delayRefresh)
        {
            if (target.HasParent)
            {
                CreateEmptyParent(target.Parent, delayRefresh);
                return;
            }

            Scene scene = target.HasScene ? target.Scene : SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            GameObject parent = new GameObject("GameObject");
            Undo.RegisterCreatedObjectUndo(parent, "Create Empty Parent");
            SceneManager.MoveGameObjectToScene(parent, scene);
            Selection.activeGameObject = parent;
            EditorSceneManager.MarkSceneDirty(scene);
            delayRefresh?.Invoke();
        }

        public static void CreateEmptyParent(GameObject target, Action delayRefresh)
        {
            if (target == null)
            {
                return;
            }

            Scene scene = target.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            Transform originalParent = target.transform.parent;
            int siblingIndex = target.transform.GetSiblingIndex();

            GameObject parentObject = new GameObject("GameObject");
            Undo.RegisterCreatedObjectUndo(parentObject, "Create Empty Parent");
            SceneManager.MoveGameObjectToScene(parentObject, scene);

            if (originalParent != null)
            {
                parentObject.transform.SetParent(originalParent, false);
                parentObject.transform.SetSiblingIndex(siblingIndex);
            }

            Undo.SetTransformParent(target.transform, parentObject.transform, "Create Empty Parent");
            parentObject.transform.position = target.transform.position;
            parentObject.transform.rotation = target.transform.rotation;
            parentObject.transform.localScale = Vector3.one;

            Selection.activeGameObject = parentObject;
            EditorSceneManager.MarkSceneDirty(scene);
            delayRefresh?.Invoke();
        }

        public static void SetSiblingIndex(GameObject target, int index, Action delayRefresh)
        {
            if (target == null || target.transform.parent == null)
            {
                return;
            }

            Transform transform = target.transform;
            int resolvedIndex = index < 0
                ? transform.parent.childCount - 1
                : Mathf.Clamp(index, 0, transform.parent.childCount - 1);

            Undo.RecordObject(transform, "Set Sibling Index");
            transform.SetSiblingIndex(resolvedIndex);

            if (target.scene.IsValid() && target.scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(target.scene);
            }

            delayRefresh?.Invoke();
        }

        public static void MoveSelectionToView()
        {
            EditorApplication.ExecuteMenuItem("GameObject/Move To View");
        }

        public static void UnpackPrefab(GameObject target, Action delayRefresh)
        {
            if (target == null || !PrefabUtility.IsPartOfPrefabInstance(target))
            {
                return;
            }

            PrefabUtility.UnpackPrefabInstance(target, PrefabUnpackMode.Completely, InteractionMode.UserAction);
            if (target.scene.IsValid() && target.scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(target.scene);
            }

            delayRefresh?.Invoke();
        }

        public static void ExecuteCreateCommand(string menuPath, ContextTarget target, Action delayRefresh)
        {
            GameObject previousSelection = Selection.activeGameObject;
            Scene previousActiveScene = SceneManager.GetActiveScene();

            try
            {
                if (target.HasScene)
                {
                    EditorSceneManager.SetActiveScene(target.Scene);
                }

                Selection.activeGameObject = target.HasParent ? target.Parent : null;

                if (!EditorApplication.ExecuteMenuItem(menuPath))
                {
                    Selection.activeGameObject = previousSelection;
                    Debug.LogWarning($"CustomHierarchy: Unity menu command unavailable: {menuPath}");
                }
            }
            finally
            {
                if (previousActiveScene.IsValid() && SceneManager.GetActiveScene() != previousActiveScene)
                {
                    EditorSceneManager.SetActiveScene(previousActiveScene);
                }

                delayRefresh?.Invoke();
            }
        }

        public static void SetActive(GameObject target, bool active, Action delayRefresh)
        {
            if (target == null)
            {
                return;
            }

            Undo.RecordObject(target, active ? "Set Active" : "Set Inactive");
            target.SetActive(active);

            if (target.scene.IsValid() && target.scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(target.scene);
            }

            delayRefresh?.Invoke();
        }

        public static void SetSceneVisibility(GameObject target, bool visible, Action delayRefresh)
        {
            if (target == null)
            {
                return;
            }

            if (visible)
            {
                SceneVisibilityManager.instance.Show(target, true);
            }
            else
            {
                SceneVisibilityManager.instance.Hide(target, true);
            }

            delayRefresh?.Invoke();
        }

        public static void FrameObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Selection.activeGameObject = target;
            EditorApplication.ExecuteMenuItem("Edit/Frame Selected");
        }

        private static List<GameObject> GetSelectedSceneObjects()
        {
            List<GameObject> result = new List<GameObject>();
            GameObject[] selected = Selection.gameObjects;
            for (int i = 0; i < selected.Length; i++)
            {
                GameObject gameObject = selected[i];
                if (gameObject == null)
                {
                    continue;
                }

                Scene scene = gameObject.scene;
                if (scene.IsValid() && scene.isLoaded)
                {
                    result.Add(gameObject);
                }
            }

            return result;
        }

        private static List<GameObject> GetTopLevelSelection(List<GameObject> selected)
        {
            List<GameObject> result = new List<GameObject>();
            for (int i = 0; i < selected.Count; i++)
            {
                GameObject candidate = selected[i];
                if (candidate == null)
                {
                    continue;
                }

                bool childOfAnotherSelected = false;
                Transform parent = candidate.transform.parent;
                while (parent != null)
                {
                    if (selected.Contains(parent.gameObject))
                    {
                        childOfAnotherSelected = true;
                        break;
                    }

                    parent = parent.parent;
                }

                if (!childOfAnotherSelected)
                {
                    result.Add(candidate);
                }
            }

            return result;
        }
    }
}
