using System;
using System.Collections.Generic;
using Glai.ECS;
using Glai.ECS.Core;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Glai.Core.Editor
{
    public sealed class CustomHierarchyWindow : EditorWindow
    {
        private TreeViewState treeState;
        private SearchField searchField;
        private CustomHierarchyTreeView treeView;
        private bool showInactive = true;
        private bool showHidden = true;
        private string searchInput = string.Empty;
        private readonly List<GameObject> copyBuffer = new List<GameObject>();

        [MenuItem("Tools/Glai/Custom Hierarchy")]
        public static void OpenWindow()
        {
            CustomHierarchyWindow window = GetWindow<CustomHierarchyWindow>("Custom Hierarchy");
            window.minSize = new Vector2(320f, 260f);
            window.Show();
        }

        private void OnEnable()
        {
            treeState ??= new TreeViewState();
            searchField ??= new SearchField();

            treeView = new CustomHierarchyTreeView(treeState)
            {
                ShowInactive = showInactive,
                ShowHidden = showHidden,
                DrawGameObjectRow = DrawCustomRow,
                ContextClickedItemCallback = HandleContextClickedItem,
                ContextClickedCallback = HandleContextClicked,
                HandleObjectsDropped = HandleObjectsDropped,
            };

            CustomHierarchySearch.Apply(treeView, searchInput, reload: true);
            EditorApplication.hierarchyChanged += HandleHierarchyChanged;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= HandleHierarchyChanged;
        }

        private void OnSelectionChange()
        {
            if (treeView == null)
            {
                return;
            }

            if (Selection.activeObject is EcsSelectionProxy proxy)
            {
                if (proxy.Kind == HierarchyTreeItem.ItemKind.EcsEntity)
                {
                    treeView.SelectEcsEntityPage(proxy.ArchetypeIndex, proxy.ChunkIndex, proxy.RangeStart);
                }

                Repaint();
                return;
            }

            List<int> selectedIds = new List<int>();
            GameObject[] selectedObjects = Selection.gameObjects;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                GameObject selectedObject = selectedObjects[i];
                if (selectedObject != null && treeView.ContainsObject(selectedObject))
                {
                    selectedIds.Add(selectedObject.GetInstanceID());
                }
            }

            treeView.SetSelection(selectedIds, TreeViewSelectionOptions.RevealAndFrame);
            Repaint();
        }

        private void OnGUI()
        {
            if (treeView == null)
            {
                OnEnable();
            }

            DrawToolbar();

            Rect treeRect = GUILayoutUtility.GetRect(0f, 100000f, 0f, 100000f);
            Event currentEvent = Event.current;

            treeView.OnGUI(treeRect);
            HandleKeyboardShortcuts(currentEvent);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string nextSearch = searchField.OnToolbarGUI(searchInput);
            if (!string.Equals(searchInput, nextSearch, StringComparison.Ordinal))
            {
                searchInput = nextSearch;
                CustomHierarchySearch.Apply(treeView, searchInput, reload: true);
            }

            bool previousShowInactive = showInactive;
            bool previousShowHidden = showHidden;

            showInactive = GUILayout.Toggle(showInactive, "Inactive", EditorStyles.toolbarButton, GUILayout.Width(70f));
            showHidden = GUILayout.Toggle(showHidden, "Hidden", EditorStyles.toolbarButton, GUILayout.Width(64f));

            if (showInactive != previousShowInactive || showHidden != previousShowHidden)
            {
                treeView.ShowInactive = showInactive;
                treeView.ShowHidden = showHidden;
                treeView.Reload();
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(66f)))
            {
                treeView.Reload();
            }

            if (GUILayout.Button("Expand", EditorStyles.toolbarButton, GUILayout.Width(58f)))
            {
                treeView.ExpandAll();
            }

            if (GUILayout.Button("Collapse", EditorStyles.toolbarButton, GUILayout.Width(66f)))
            {
                treeView.CollapseAll();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCustomRow(Rect rowRect, HierarchyTreeItem item, bool isInactive, bool isHidden)
        {
            const float controlWidth = 44f;
            const float spacing = 4f;

            Rect visibilityRect = new Rect(rowRect.xMax - controlWidth, rowRect.y + 1f, controlWidth, rowRect.height - 2f);
            Rect activeRect = new Rect(visibilityRect.x - controlWidth - spacing, rowRect.y + 1f, controlWidth, rowRect.height - 2f);

            bool nextActive = GUI.Toggle(activeRect, !isInactive, new GUIContent("Act", "Toggle active state"), EditorStyles.miniButton);
            if (nextActive == isInactive)
            {
                CustomHierarchyActions.SetActive(item.GameObject, nextActive, DelayRefresh);
                Event.current.Use();
            }

            bool nextVisible = GUI.Toggle(visibilityRect, !isHidden, new GUIContent("Vis", "Toggle scene visibility"), EditorStyles.miniButton);
            if (nextVisible == isHidden)
            {
                CustomHierarchyActions.SetSceneVisibility(item.GameObject, nextVisible, DelayRefresh);
                Event.current.Use();
            }

            if (isHidden)
            {
                Rect badgeRect = new Rect(activeRect.x - 58f, rowRect.y + 2f, 54f, rowRect.height - 4f);
                EditorGUI.LabelField(badgeRect, "HIDDEN", EditorStyles.miniBoldLabel);
                return;
            }

            if (isInactive)
            {
                Rect badgeRect = new Rect(activeRect.x - 66f, rowRect.y + 2f, 62f, rowRect.height - 4f);
                EditorGUI.LabelField(badgeRect, "INACTIVE", EditorStyles.miniBoldLabel);
            }
        }

        private void HandleHierarchyChanged()
        {
            if (treeView == null)
            {
                return;
            }

            treeView.Reload();
            Repaint();
        }

        private void HandleContextClickedItem(int id)
        {
            if (treeView == null || !treeView.TryGetHierarchyItem(id, out HierarchyTreeItem clickedItem))
            {
                return;
            }

            if (!clickedItem.IsSceneHeader && clickedItem.GameObject != null)
            {
                Selection.activeGameObject = clickedItem.GameObject;
            }
            else if (!clickedItem.IsSceneHeader)
            {
                return;
            }

            GenericMenu menu = new GenericMenu();
            ContextTarget target = clickedItem.IsSceneHeader
                ? new ContextTarget(null, clickedItem.Scene)
                : new ContextTarget(clickedItem.GameObject, clickedItem.GameObject.scene);

            CustomHierarchyMenus.BuildCreateMenu(menu, target, !clickedItem.IsSceneHeader, ExecuteCreateCommand, CreateEmptyParentForSelection);

            if (!clickedItem.IsSceneHeader && clickedItem.GameObject != null)
            {
                menu.AddSeparator(string.Empty);
                CustomHierarchyMenus.AddObjectActions(menu, clickedItem.GameObject, copyBuffer.Count > 0, RenameObject, DuplicateSelection, DeleteSelection, CreateEmptyParent, SetSiblingIndex, MoveSelectionToView, UnpackPrefab, CopySelection, PasteSelection, SetActive);
            }

            menu.ShowAsContext();
        }

        private void HandleContextClicked()
        {
            GenericMenu menu = new GenericMenu();
            GameObject selectedObject = Selection.activeGameObject;
            Scene targetScene = selectedObject != null && selectedObject.scene.IsValid() && selectedObject.scene.isLoaded
                ? selectedObject.scene
                : SceneManager.GetActiveScene();

            CustomHierarchyMenus.BuildCreateMenu(menu, new ContextTarget(selectedObject, targetScene), selectedObject != null, ExecuteCreateCommand, CreateEmptyParentForSelection);
            menu.AddSeparator(string.Empty);
            CustomHierarchyMenus.AddGeneralEditActions(menu, copyBuffer.Count > 0, RenameSelection, DuplicateSelection, DeleteSelection, CopySelection, PasteActiveSelection, CreateEmptyParentFromActiveSelection, MoveSelectionToView);
            menu.ShowAsContext();
        }

        private bool HandleObjectsDropped(IReadOnlyList<GameObject> draggedObjects, int targetId, int insertAtIndex, bool performDrop)
        {
            return CustomHierarchyActions.HandleObjectsDropped(treeView, draggedObjects, targetId, insertAtIndex, performDrop, DelayRefresh);
        }

        private void ClearSelection()
        {
            CustomHierarchyActions.ClearSelection(treeView);
            Repaint();
        }

        private void CreateEmptyParentForSelection(ContextTarget target)
        {
            CustomHierarchyActions.CreateEmptyParentForSelection(target, DelayRefresh);
        }

        private void CreateEmptyParent(GameObject target)
        {
            CustomHierarchyActions.CreateEmptyParent(target, DelayRefresh);
        }

        private void SetSiblingIndex(GameObject target, int index)
        {
            CustomHierarchyActions.SetSiblingIndex(target, index, DelayRefresh);
        }

        private static void MoveSelectionToView()
        {
            CustomHierarchyActions.MoveSelectionToView();
        }

        private void UnpackPrefab(GameObject target)
        {
            CustomHierarchyActions.UnpackPrefab(target, DelayRefresh);
        }

        private void ExecuteCreateCommand(string menuPath, ContextTarget target)
        {
            CustomHierarchyActions.ExecuteCreateCommand(menuPath, target, DelayRefresh);
        }

        private void RenameObject(GameObject target)
        {
            CustomHierarchyActions.RenameObject(target, treeView);
        }

        private void RenameSelection()
        {
            RenameObject(Selection.activeGameObject);
        }

        private void DeleteSelection()
        {
            CustomHierarchyActions.DeleteSelection(treeView, DelayRefresh);
            Repaint();
        }

        private void DuplicateSelection()
        {
            CustomHierarchyActions.DuplicateSelection(DelayRefresh);
        }

        private void CopySelection()
        {
            CustomHierarchyActions.CopySelection(copyBuffer);
        }

        private void PasteSelection(GameObject preferredParent, Scene fallbackScene)
        {
            CustomHierarchyActions.PasteSelection(copyBuffer, preferredParent, fallbackScene, DelayRefresh);
        }

        private void PasteActiveSelection()
        {
            PasteSelection(Selection.activeGameObject, SceneManager.GetActiveScene());
        }

        private void CreateEmptyParentFromActiveSelection()
        {
            CreateEmptyParent(Selection.activeGameObject);
        }

        private void SetActive(GameObject target, bool active)
        {
            CustomHierarchyActions.SetActive(target, active, DelayRefresh);
        }

        private void HandleKeyboardShortcuts(Event currentEvent)
        {
            if (currentEvent == null || currentEvent.type != EventType.KeyDown)
            {
                return;
            }

            bool actionModifier = currentEvent.control || currentEvent.command;

            if (currentEvent.keyCode == KeyCode.Delete || currentEvent.keyCode == KeyCode.Backspace)
            {
                DeleteSelection();
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.F2)
            {
                RenameSelection();
                currentEvent.Use();
                return;
            }

            if (!actionModifier)
            {
                return;
            }

            if (currentEvent.keyCode == KeyCode.C)
            {
                CopySelection();
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.V)
            {
                PasteActiveSelection();
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.D)
            {
                DuplicateSelection();
                currentEvent.Use();
            }
        }

        private void DelayRefresh()
        {
            EditorApplication.delayCall += () =>
            {
                if (treeView == null)
                {
                    return;
                }

                treeView.Reload();
                Repaint();
            };
        }
    }
}
