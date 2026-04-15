using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Glai.Core.Editor
{
    public sealed class CustomHierarchyWindow : EditorWindow
    {
        private sealed class HierarchyTreeItem : TreeViewItem
        {
            public readonly GameObject GameObject;
            public readonly Scene Scene;
            public readonly bool IsSceneHeader;

            public HierarchyTreeItem(int id, int depth, string displayName, Scene scene)
                : base(id, depth, displayName)
            {
                Scene = scene;
                IsSceneHeader = true;
            }

            public HierarchyTreeItem(int id, int depth, string displayName, GameObject gameObject)
                : base(id, depth, displayName)
            {
                GameObject = gameObject;
                IsSceneHeader = false;
            }
        }

        private sealed class CustomHierarchyTreeView : TreeView
        {
            private const int SceneHeaderBaseId = -1000000;
            private readonly List<TreeViewItem> rootChildren = new List<TreeViewItem>();

            public bool ShowInactive { get; set; } = true;
            public bool ShowHidden { get; set; } = true;
            public Func<GameObject, bool> AdditionalFilter { get; set; }
            public Action<Rect, HierarchyTreeItem, bool, bool> DrawGameObjectRow { get; set; }
            public Action<int> ContextClickedItemCallback { get; set; }
            public Action ContextClickedCallback { get; set; }
            public Func<IReadOnlyList<GameObject>, int, bool, bool> HandleObjectsDropped { get; set; }

            public CustomHierarchyTreeView(TreeViewState state)
                : base(state)
            {
                showBorder = true;
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                TreeViewItem root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
                rootChildren.Clear();

                int sceneCount = SceneManager.sceneCount;
                for (int i = 0; i < sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (!scene.IsValid() || !scene.isLoaded)
                    {
                        continue;
                    }

                    string sceneDisplayName = string.IsNullOrEmpty(scene.name) ? "untitled" : scene.name;
                    int sceneObjectCount = CountSceneObjects(scene);
                    sceneDisplayName = $"{sceneDisplayName} ({sceneObjectCount})";
                    HierarchyTreeItem sceneItem = new HierarchyTreeItem(SceneHeaderBaseId - scene.handle, 0, sceneDisplayName, scene);
                    sceneItem.children = new List<TreeViewItem>();

                    GameObject[] roots = scene.GetRootGameObjects();
                    for (int r = 0; r < roots.Length; r++)
                    {
                        HierarchyTreeItem objectItem = BuildGameObjectItem(roots[r], 1);
                        if (objectItem != null)
                        {
                            sceneItem.children.Add(objectItem);
                        }
                    }

                    if (sceneItem.children.Count > 0 || string.IsNullOrEmpty(searchString))
                    {
                        rootChildren.Add(sceneItem);
                    }
                }

                if (rootChildren.Count == 0)
                {
                    rootChildren.Add(new TreeViewItem { id = int.MinValue + 1, depth = 0, displayName = "No loaded scenes or matching objects." });
                }

                SetupParentsAndChildrenFromDepths(root, rootChildren);
                return root;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                HierarchyTreeItem item = args.item as HierarchyTreeItem;
                if (item == null || item.IsSceneHeader || item.GameObject == null)
                {
                    base.RowGUI(args);
                    return;
                }

                bool isInactive = !item.GameObject.activeSelf;
                bool isHidden = SceneVisibilityManager.instance.IsHidden(item.GameObject);

                if (DrawGameObjectRow != null)
                {
                    DrawGameObjectRow(args.rowRect, item, isInactive, isHidden);
                }

                base.RowGUI(args);
            }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                if (selectedIds == null || selectedIds.Count == 0)
                {
                    return;
                }

                TreeViewItem treeItem = FindItem(selectedIds[0], rootItem);
                HierarchyTreeItem item = treeItem as HierarchyTreeItem;
                if (item == null || item.IsSceneHeader || item.GameObject == null)
                {
                    return;
                }

                if (Selection.activeObject != item.GameObject)
                {
                    Selection.activeObject = item.GameObject;
                    EditorGUIUtility.PingObject(item.GameObject);
                }
            }

            protected override void ContextClickedItem(int id)
            {
                ContextClickedItemCallback?.Invoke(id);
            }

            protected override void ContextClicked()
            {
                ContextClickedCallback?.Invoke();
            }

            protected override bool CanRename(TreeViewItem item)
            {
                HierarchyTreeItem hierarchyItem = item as HierarchyTreeItem;
                return hierarchyItem != null && !hierarchyItem.IsSceneHeader && hierarchyItem.GameObject != null;
            }

            protected override void RenameEnded(RenameEndedArgs args)
            {
                if (!args.acceptedRename)
                {
                    return;
                }

                if (!TryGetHierarchyItem(args.itemID, out HierarchyTreeItem item) || item.IsSceneHeader || item.GameObject == null)
                {
                    return;
                }

                string newName = string.IsNullOrWhiteSpace(args.newName) ? item.GameObject.name : args.newName.Trim();
                if (string.Equals(newName, item.GameObject.name, StringComparison.Ordinal))
                {
                    return;
                }

                Undo.RecordObject(item.GameObject, "Rename GameObject");
                item.GameObject.name = newName;

                if (item.GameObject.scene.IsValid() && item.GameObject.scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(item.GameObject.scene);
                }

                Reload();
            }

            protected override bool CanStartDrag(CanStartDragArgs args)
            {
                if (args.draggedItemIDs == null || args.draggedItemIDs.Count == 0)
                {
                    return false;
                }

                for (int i = 0; i < args.draggedItemIDs.Count; i++)
                {
                    if (TryGetHierarchyItem(args.draggedItemIDs[i], out HierarchyTreeItem item) && !item.IsSceneHeader)
                    {
                        return true;
                    }
                }

                return false;
            }

            protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
            {
                List<GameObject> draggedObjects = CollectDraggedObjects(args.draggedItemIDs);
                if (draggedObjects.Count == 0)
                {
                    return;
                }

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = draggedObjects.ToArray();
                DragAndDrop.paths = null;
                DragAndDrop.StartDrag("Custom Hierarchy Drag");
            }

            protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
            {
                if (HandleObjectsDropped == null)
                {
                    return DragAndDropVisualMode.Rejected;
                }

                List<GameObject> draggedObjects = CollectDraggedObjectsFromDragAndDrop();
                if (draggedObjects.Count == 0)
                {
                    return DragAndDropVisualMode.None;
                }

                int targetId = args.parentItem != null ? args.parentItem.id : 0;
                bool accepted = HandleObjectsDropped(draggedObjects, targetId, args.performDrop);
                return accepted ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
            }

            public bool ContainsObject(GameObject gameObject)
            {
                if (gameObject == null)
                {
                    return false;
                }

                return FindItem(gameObject.GetInstanceID(), rootItem) != null;
            }

            public bool TryGetHierarchyItem(int id, out HierarchyTreeItem item)
            {
                item = FindItem(id, rootItem) as HierarchyTreeItem;
                return item != null;
            }

            public void BeginRenameById(int id)
            {
                if (!TryGetHierarchyItem(id, out HierarchyTreeItem item) || item.IsSceneHeader)
                {
                    return;
                }

                BeginRename(item);
            }

            public bool IsMouseOverAnyRow(Vector2 mousePosition)
            {
                IList<TreeViewItem> rows = GetRows();
                for (int i = 0; i < rows.Count; i++)
                {
                    if (GetRowRect(i).Contains(mousePosition))
                    {
                        return true;
                    }
                }

                return false;
            }

            private List<GameObject> CollectDraggedObjects(IList<int> draggedItemIds)
            {
                List<GameObject> draggedObjects = new List<GameObject>();
                if (draggedItemIds == null)
                {
                    return draggedObjects;
                }

                for (int i = 0; i < draggedItemIds.Count; i++)
                {
                    if (!TryGetHierarchyItem(draggedItemIds[i], out HierarchyTreeItem item) || item.IsSceneHeader)
                    {
                        continue;
                    }

                    if (item.GameObject != null)
                    {
                        draggedObjects.Add(item.GameObject);
                    }
                }

                return draggedObjects;
            }

            private static List<GameObject> CollectDraggedObjectsFromDragAndDrop()
            {
                List<GameObject> draggedObjects = new List<GameObject>();
                UnityEngine.Object[] references = DragAndDrop.objectReferences;
                if (references == null)
                {
                    return draggedObjects;
                }

                for (int i = 0; i < references.Length; i++)
                {
                    GameObject gameObject = references[i] as GameObject;
                    if (gameObject != null)
                    {
                        draggedObjects.Add(gameObject);
                    }
                }

                return draggedObjects;
            }

            private HierarchyTreeItem BuildGameObjectItem(GameObject gameObject, int depth)
            {
                bool nameMatch = string.IsNullOrEmpty(searchString) ||
                                 gameObject.name.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0;

                bool visibleByState = (ShowInactive || gameObject.activeSelf) &&
                                      (ShowHidden || !SceneVisibilityManager.instance.IsHidden(gameObject));

                bool passesAdditional = AdditionalFilter == null || AdditionalFilter(gameObject);

                List<TreeViewItem> childItems = null;
                Transform transform = gameObject.transform;
                int childCount = transform.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    HierarchyTreeItem childItem = BuildGameObjectItem(transform.GetChild(i).gameObject, depth + 1);
                    if (childItem != null)
                    {
                        if (childItems == null)
                        {
                            childItems = new List<TreeViewItem>();
                        }

                        childItems.Add(childItem);
                    }
                }

                bool includeBySelf = nameMatch && visibleByState && passesAdditional;
                bool includeByChildren = childItems != null && childItems.Count > 0;
                if (!includeBySelf && !includeByChildren)
                {
                    return null;
                }

                HierarchyTreeItem item = new HierarchyTreeItem(gameObject.GetInstanceID(), depth, gameObject.name, gameObject)
                {
                    children = childItems,
                };

                return item;
            }

            private static int CountSceneObjects(Scene scene)
            {
                int count = 0;
                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    count += CountHierarchy(roots[i].transform);
                }

                return count;
            }

            private static int CountHierarchy(Transform root)
            {
                int count = 1;
                int childCount = root.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    count += CountHierarchy(root.GetChild(i));
                }

                return count;
            }
        }

        private TreeViewState treeState;
        private SearchField searchField;
        private CustomHierarchyTreeView treeView;
        private bool showInactive = true;
        private bool showHidden = true;
        private string searchInput = string.Empty;
        private readonly List<GameObject> copyBuffer = new List<GameObject>();

        private readonly struct SearchQuery
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

        private readonly struct ContextTarget
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

            "GameObject/2D Object/Sprite",
            "GameObject/2D Object/Sprite Shape",
            "GameObject/2D Object/Tilemap/Rectangular",
            "GameObject/2D Object/Tilemap/Hexagonal Point Top",
            "GameObject/2D Object/Tilemap/Hexagonal Flat Top",
            "GameObject/2D Object/Tilemap/Isometric",
            "GameObject/2D Object/Tilemap/Isometric Z as Y",

            "GameObject/Effects/Particle System",
            "GameObject/Effects/Trail",
            "GameObject/Effects/Line",

            "GameObject/Light/Directional Light",
            "GameObject/Light/Point Light",
            "GameObject/Light/Spotlight",
            "GameObject/Light/Area Light",
            "GameObject/Light/Reflection Probe",
            "GameObject/Light/Light Probe Group",

            "GameObject/Audio/Audio Source",
            "GameObject/Audio/Audio Reverb Zone",

            "GameObject/Video/Video Player",
            "GameObject/Camera",
            "GameObject/Cinemachine/Camera",

            "GameObject/UI/Canvas",
            "GameObject/UI/UI Toolkit/EventSystem",
            "GameObject/UI/Text - TextMeshPro",
            "GameObject/UI/Image",
            "GameObject/UI/Raw Image",
            "GameObject/UI/Button - TextMeshPro",
            "GameObject/UI/Legacy/Button",
            "GameObject/UI/Toggle",
            "GameObject/UI/Slider",
            "GameObject/UI/Scrollbar",
            "GameObject/UI/Scroll View",
            "GameObject/UI/Input Field - TextMeshPro",
            "GameObject/UI/Legacy/Input Field",
            "GameObject/UI/Dropdown - TextMeshPro",
            "GameObject/UI/Legacy/Dropdown",
            "GameObject/UI/Panel",
            "GameObject/UI/Event System",

            "GameObject/Visual Effects/Visual Effect",
            "GameObject/Volume/Global Volume",
            "GameObject/Volume/Local Volume",
            "GameObject/XR/Room-Scale XR Rig",
            "GameObject/XR/XR Origin (VR)",
            "GameObject/XR/XR Origin (AR)",
        };

        [MenuItem("Tools/Glai/Custom Hierarchy")]
        public static void OpenWindow()
        {
            CustomHierarchyWindow window = GetWindow<CustomHierarchyWindow>("Custom Hierarchy");
            window.minSize = new Vector2(320f, 260f);
            window.Show();
        }

        private void OnEnable()
        {
            if (treeState == null)
            {
                treeState = new TreeViewState();
            }

            if (searchField == null)
            {
                searchField = new SearchField();
            }

            treeView = new CustomHierarchyTreeView(treeState)
            {
                ShowInactive = showInactive,
                ShowHidden = showHidden,
                DrawGameObjectRow = DrawCustomRow,
                ContextClickedItemCallback = HandleContextClickedItem,
                ContextClickedCallback = HandleContextClicked,
                HandleObjectsDropped = HandleObjectsDropped,
            };

            ApplySearch(searchInput, reload: true);

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

            GameObject selectedGameObject = Selection.activeGameObject;
            if (selectedGameObject == null)
            {
                treeView.SetSelection(new List<int>());
                Repaint();
                return;
            }

            if (!treeView.ContainsObject(selectedGameObject))
            {
                return;
            }

            treeView.SetSelection(new List<int> { selectedGameObject.GetInstanceID() }, TreeViewSelectionOptions.RevealAndFrame);
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
            bool isEmptyLeftClick = currentEvent.type == EventType.MouseDown &&
                                    currentEvent.button == 0 &&
                                    treeRect.Contains(currentEvent.mousePosition);

            treeView.OnGUI(treeRect);

            if (isEmptyLeftClick && !treeView.IsMouseOverAnyRow(currentEvent.mousePosition))
            {
                ClearSelection();
            }

            HandleKeyboardShortcuts(currentEvent);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            string nextSearch = searchField.OnToolbarGUI(searchInput);
            if (!string.Equals(searchInput, nextSearch, StringComparison.Ordinal))
            {
                searchInput = nextSearch;
                ApplySearch(searchInput, reload: true);
            }

            bool previousShowInactive = showInactive;
            showInactive = GUILayout.Toggle(showInactive, "Inactive", EditorStyles.toolbarButton, GUILayout.Width(70f));

            bool previousShowHidden = showHidden;
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
                SetActive(item.GameObject, nextActive);
                Event.current.Use();
            }

            bool nextVisible = GUI.Toggle(visibilityRect, !isHidden, new GUIContent("Vis", "Toggle scene visibility"), EditorStyles.miniButton);
            if (nextVisible == isHidden)
            {
                SetSceneVisibility(item.GameObject, nextVisible);
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
            if (treeView == null)
            {
                return;
            }

            if (!treeView.TryGetHierarchyItem(id, out HierarchyTreeItem clickedItem))
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

            BuildCreateMenu(menu, target, includeCreateEmptyChild: !clickedItem.IsSceneHeader);

            if (!clickedItem.IsSceneHeader && clickedItem.GameObject != null)
            {
                menu.AddSeparator(string.Empty);
                AddObjectActions(menu, clickedItem.GameObject);
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

            BuildCreateMenu(menu, new ContextTarget(selectedObject, targetScene), includeCreateEmptyChild: selectedObject != null);

            menu.AddSeparator(string.Empty);
            AddGeneralEditActions(menu);

            menu.ShowAsContext();
        }

        private bool HandleObjectsDropped(IReadOnlyList<GameObject> draggedObjects, int targetId, bool performDrop)
        {
            if (draggedObjects == null || draggedObjects.Count == 0)
            {
                return false;
            }

            GameObject targetParent = null;
            Scene targetScene = SceneManager.GetActiveScene();

            if (targetId != 0 && treeView != null && treeView.TryGetHierarchyItem(targetId, out HierarchyTreeItem targetItem))
            {
                if (targetItem.IsSceneHeader)
                {
                    targetScene = targetItem.Scene;
                }
                else
                {
                    targetParent = targetItem.GameObject;
                    targetScene = targetParent.scene;
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

                if (targetParent != null)
                {
                    if (dragged == targetParent)
                    {
                        return false;
                    }

                    if (targetParent.transform.IsChildOf(dragged.transform))
                    {
                        return false;
                    }
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
            DelayRefresh();
            return true;
        }

        private void ClearSelection()
        {
            if (treeView != null)
            {
                treeView.SetSelection(new List<int>());
            }

            if (Selection.activeObject != null)
            {
                Selection.activeObject = null;
            }

            Repaint();
        }

        private void BuildCreateMenu(GenericMenu menu, ContextTarget target, bool includeCreateEmptyChild)
        {
            for (int i = 0; i < CreateCommands.Length; i++)
            {
                string command = CreateCommands[i];
                if (!includeCreateEmptyChild && string.Equals(command, "GameObject/Create Empty Child", StringComparison.Ordinal))
                {
                    continue;
                }

                string displayPath = command.Substring("GameObject/".Length);
                menu.AddItem(
                    new GUIContent($"Create/{displayPath}"),
                    false,
                    () => ExecuteCreateCommand(command, target));
            }

            menu.AddSeparator("Create/");
            menu.AddItem(new GUIContent("Create/Empty Parent"), false, () => CreateEmptyParentForSelection(target));
        }

        private void AddObjectActions(GenericMenu menu, GameObject target)
        {
            menu.AddItem(new GUIContent("Rename"), false, () => RenameObject(target));
            menu.AddItem(new GUIContent("Duplicate"), false, DuplicateSelection);
            menu.AddItem(new GUIContent("Delete"), false, DeleteSelection);

            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Create Empty Parent"), false, () => CreateEmptyParent(target));

            if (target.transform.parent != null)
            {
                menu.AddItem(new GUIContent("Set As First Sibling"), false, () => SetSiblingIndex(target, 0));
                menu.AddItem(new GUIContent("Set As Last Sibling"), false, () => SetSiblingIndex(target, -1));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Set As First Sibling"));
                menu.AddDisabledItem(new GUIContent("Set As Last Sibling"));
            }

            menu.AddItem(new GUIContent("Move To View"), false, MoveSelectionToView);

            if (PrefabUtility.IsPartOfPrefabInstance(target))
            {
                menu.AddItem(new GUIContent("Unpack Prefab Completely"), false, () => UnpackPrefab(target));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Unpack Prefab Completely"));
            }

            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Copy"), false, CopySelection);
            if (copyBuffer.Count > 0)
            {
                menu.AddItem(new GUIContent("Paste"), false, () => PasteSelection(target, target.scene));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }

            menu.AddSeparator(string.Empty);

            bool isActive = target.activeSelf;
            if (isActive)
            {
                menu.AddItem(new GUIContent("Set Inactive"), false, () => SetActive(target, false));
            }
            else
            {
                menu.AddItem(new GUIContent("Set Active"), false, () => SetActive(target, true));
            }

            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Ping"), false, () => EditorGUIUtility.PingObject(target));
            menu.AddItem(new GUIContent("Frame Selected"), false, () => FrameObject(target));
        }

        private static void FrameObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            Selection.activeGameObject = target;
            EditorApplication.ExecuteMenuItem("Edit/Frame Selected");
        }

        private void AddGeneralEditActions(GenericMenu menu)
        {
            bool hasSelection = GetSelectedSceneObjects().Count > 0;

            if (hasSelection)
            {
                menu.AddItem(new GUIContent("Rename"), false, RenameSelection);
                menu.AddItem(new GUIContent("Duplicate"), false, DuplicateSelection);
                menu.AddItem(new GUIContent("Delete"), false, DeleteSelection);
                menu.AddItem(new GUIContent("Copy"), false, CopySelection);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Rename"));
                menu.AddDisabledItem(new GUIContent("Duplicate"));
                menu.AddDisabledItem(new GUIContent("Delete"));
                menu.AddDisabledItem(new GUIContent("Copy"));
            }

            if (copyBuffer.Count > 0)
            {
                menu.AddItem(new GUIContent("Paste"), false, () => PasteSelection(Selection.activeGameObject, SceneManager.GetActiveScene()));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Paste"));
            }

            menu.AddSeparator(string.Empty);
            if (Selection.activeGameObject != null)
            {
                menu.AddItem(new GUIContent("Create Empty Parent"), false, () => CreateEmptyParent(Selection.activeGameObject));
                menu.AddItem(new GUIContent("Move To View"), false, MoveSelectionToView);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Create Empty Parent"));
                menu.AddDisabledItem(new GUIContent("Move To View"));
            }
        }

        private void CreateEmptyParentForSelection(ContextTarget target)
        {
            if (target.HasParent)
            {
                CreateEmptyParent(target.Parent);
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
            DelayRefresh();
        }

        private void CreateEmptyParent(GameObject target)
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
            DelayRefresh();
        }

        private void SetSiblingIndex(GameObject target, int index)
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

            DelayRefresh();
        }

        private static void MoveSelectionToView()
        {
            EditorApplication.ExecuteMenuItem("GameObject/Move To View");
        }

        private void UnpackPrefab(GameObject target)
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

            DelayRefresh();
        }

        private void ExecuteCreateCommand(string menuPath, ContextTarget target)
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

                bool executed = EditorApplication.ExecuteMenuItem(menuPath);
                if (!executed)
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

                DelayRefresh();
            }
        }

        private void RenameObject(GameObject target)
        {
            if (target == null || treeView == null)
            {
                return;
            }

            Selection.activeGameObject = target;
            treeView.SetSelection(new List<int> { target.GetInstanceID() }, TreeViewSelectionOptions.RevealAndFrame);
            treeView.BeginRenameById(target.GetInstanceID());
        }

        private void RenameSelection()
        {
            RenameObject(Selection.activeGameObject);
        }

        private void DeleteSelection()
        {
            List<GameObject> selected = GetTopLevelSelection(GetSelectedSceneObjects());
            if (selected.Count == 0)
            {
                return;
            }

            for (int i = 0; i < selected.Count; i++)
            {
                GameObject gameObject = selected[i];
                if (gameObject == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(gameObject);
            }

            ClearSelection();
            DelayRefresh();
        }

        private void DuplicateSelection()
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

                int siblingIndex = source.transform.GetSiblingIndex();
                clone.transform.SetSiblingIndex(siblingIndex + 1);

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
                DelayRefresh();
            }
        }

        private void CopySelection()
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

        private void PasteSelection(GameObject preferredParent, Scene fallbackScene)
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
                DelayRefresh();
            }
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
                PasteSelection(Selection.activeGameObject, SceneManager.GetActiveScene());
                currentEvent.Use();
                return;
            }

            if (currentEvent.keyCode == KeyCode.D)
            {
                DuplicateSelection();
                currentEvent.Use();
            }
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

        private void SetActive(GameObject target, bool active)
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

            DelayRefresh();
        }

        private void SetSceneVisibility(GameObject target, bool visible)
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

            DelayRefresh();
        }

        private void ApplySearch(string rawSearch, bool reload)
        {
            if (treeView == null)
            {
                return;
            }

            SearchQuery query = ParseSearchQuery(rawSearch);
            treeView.searchString = query.NameContains;
            treeView.AdditionalFilter = query.HasStructuredTerms ? (Func<GameObject, bool>)(go => MatchesSearchQuery(go, query)) : null;

            if (reload)
            {
                treeView.Reload();
            }
        }

        private static SearchQuery ParseSearchQuery(string rawSearch)
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
                }
                else if (key == "tag")
                {
                    tagEquals = value;
                }
                else if (key == "l" || key == "layer")
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
                }
                else if (key == "name")
                {
                    freeText.Add(value);
                }
                else
                {
                    freeText.Add(token);
                }
            }

            string nameContains = string.Join(" ", freeText);
            return new SearchQuery(nameContains, typeContains, tagEquals, layerEquals);
        }

        private static bool MatchesSearchQuery(GameObject gameObject, SearchQuery query)
        {
            if (!string.IsNullOrEmpty(query.TypeContains) && !HasComponentTypeLike(gameObject, query.TypeContains))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(query.TagEquals) && !string.Equals(gameObject.tag, query.TagEquals, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (query.LayerEquals.HasValue && gameObject.layer != query.LayerEquals.Value)
            {
                return false;
            }

            return true;
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

                string typeName = component.GetType().Name;
                if (typeName.IndexOf(typeContains, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
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
