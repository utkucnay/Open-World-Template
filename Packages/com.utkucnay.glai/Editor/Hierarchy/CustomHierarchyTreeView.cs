using System;
using System.Collections.Generic;
using Glai.ECS;
using Glai.ECS.Core;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Glai.Core.Editor
{
    internal sealed class HierarchyTreeItem : TreeViewItem
    {
        public enum ItemKind
        {
            GameObject,
            SceneHeader,
            EcsRoot,
            EcsArchetype,
            EcsChunk,
            EcsEntityPage,
            EcsEntity,
        }

        public readonly ItemKind Kind;
        public readonly GameObject GameObject;
        public readonly Scene Scene;
        public readonly int VisibleObjectCount;
        public readonly int ArchetypeIndex;
        public readonly int ChunkIndex;
        public readonly int EntityCount;
        public readonly int Capacity;
        public readonly int RangeStart;
        public readonly int RangeLength;
        public readonly int EntityId;

        public bool IsSceneHeader => Kind == ItemKind.SceneHeader;
        public bool IsGameObject => Kind == ItemKind.GameObject;
        public bool IsEcsNode =>
            Kind == ItemKind.EcsRoot ||
            Kind == ItemKind.EcsArchetype ||
            Kind == ItemKind.EcsChunk ||
            Kind == ItemKind.EcsEntityPage ||
            Kind == ItemKind.EcsEntity;

        public HierarchyTreeItem(int id, int depth, string displayName, Scene scene, int visibleObjectCount)
            : base(id, depth, displayName)
        {
            Kind = ItemKind.SceneHeader;
            Scene = scene;
            VisibleObjectCount = visibleObjectCount;
            ArchetypeIndex = -1;
            ChunkIndex = -1;
            RangeStart = -1;
            RangeLength = 0;
            EntityId = -1;
        }

        public HierarchyTreeItem(int id, int depth, string displayName, GameObject gameObject)
            : base(id, depth, displayName)
        {
            Kind = ItemKind.GameObject;
            GameObject = gameObject;
            ArchetypeIndex = -1;
            ChunkIndex = -1;
            RangeStart = -1;
            RangeLength = 0;
            EntityId = -1;
        }

        public HierarchyTreeItem(int id, int depth, string displayName, ItemKind kind, int archetypeIndex, int chunkIndex, int entityCount, int capacity, int rangeStart = -1, int rangeLength = 0, int entityId = -1)
            : base(id, depth, displayName)
        {
            Kind = kind;
            ArchetypeIndex = archetypeIndex;
            ChunkIndex = chunkIndex;
            EntityCount = entityCount;
            Capacity = capacity;
            RangeStart = rangeStart;
            RangeLength = rangeLength;
            EntityId = entityId;
        }
    }

    internal sealed class CustomHierarchyTreeView : TreeView
    {
        private const int SceneHeaderBaseId = -1000000;
        private const int EcsRootId = -200000000;
        private const int EcsArchetypeBaseId = -201000000;
        private const int EcsChunkBaseId = -202000000;
        private const int EcsEntityPageBaseId = -203000000;
        private const int EcsEntityBaseId = -210000000;
        private const int EcsEntitiesPerPage = 256;
        private const int MaxChunksPerArchetype = 500;
        private const int MaxEntitiesPerChunk = 16 * 1024;
        private const int MaxPagesPerChunk = MaxEntitiesPerChunk / EcsEntitiesPerPage;
        private readonly List<TreeViewItem> rootChildren = new List<TreeViewItem>();

        public bool ShowInactive { get; set; } = true;
        public bool ShowHidden { get; set; } = true;
        public Func<GameObject, bool> AdditionalFilter { get; set; }
        public Action<Rect, HierarchyTreeItem, bool, bool> DrawGameObjectRow { get; set; }
        public Action<int> ContextClickedItemCallback { get; set; }
        public Action ContextClickedCallback { get; set; }
        public Func<IReadOnlyList<GameObject>, int, int, bool, bool> HandleObjectsDropped { get; set; }

        public CustomHierarchyTreeView(TreeViewState state)
            : base(state)
        {
            rowHeight = 20f;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
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

                AddSceneItem(scene, "untitled");
            }

            if (TryGetDontDestroyOnLoadScene(out Scene dontDestroyScene))
            {
                AddSceneItem(dontDestroyScene, "DontDestroyOnLoad");
            }

            AddEcsSummaryItem();

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
            if (item == null)
            {
                base.RowGUI(args);
                return;
            }

            if (item.IsSceneHeader)
            {
                CustomHierarchyStyles.DrawSceneHeader(args.rowRect, item);
                return;
            }

            if (item.IsEcsNode)
            {
                DrawEcsRow(args.rowRect, item, args.selected);
                return;
            }

            if (item.GameObject == null)
            {
                base.RowGUI(args);
                return;
            }

            bool isInactive = !item.GameObject.activeSelf;
            bool isHidden = SceneVisibilityManager.instance.IsHidden(item.GameObject);
            DrawGameObjectRow?.Invoke(args.rowRect, item, isInactive, isHidden);

            Rect labelRect = args.rowRect;
            labelRect.xMin += GetContentIndent(item);
            labelRect.xMax -= 96f;

            GUIContent labelContent = EditorGUIUtility.ObjectContent(item.GameObject, typeof(GameObject));
            labelContent.text = item.displayName;
            EditorGUI.LabelField(labelRect, labelContent, CustomHierarchyStyles.GetObjectLabelStyle(item.GameObject, args.selected));
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds == null)
            {
                return;
            }

            if (selectedIds.Count == 0)
            {
                EcsSceneSelectionHighlighter.Clear();

                if (Selection.objects.Length > 0)
                {
                    Selection.objects = Array.Empty<UnityEngine.Object>();
                }

                return;
            }

            if (TryGetHierarchyItem(selectedIds[0], out HierarchyTreeItem ecsItem) && ecsItem.IsEcsNode)
            {
                EcsSelectionProxy proxy = EcsSelectionProxy.GetOrCreate();

                proxy.Apply(ecsItem);

                if (Selection.activeObject != proxy)
                {
                    Selection.activeObject = proxy;
                }

                return;
            }

            List<UnityEngine.Object> selectedObjects = new List<UnityEngine.Object>(selectedIds.Count);
            for (int i = 0; i < selectedIds.Count; i++)
            {
                if (!TryGetHierarchyItem(selectedIds[i], out HierarchyTreeItem selectedItem) ||
                    selectedItem.IsSceneHeader ||
                    selectedItem.GameObject == null)
                {
                    continue;
                }

                selectedObjects.Add(selectedItem.GameObject);
            }

            EcsSceneSelectionHighlighter.Clear();

            if (selectedObjects.Count == 0)
            {
                if (Selection.objects.Length > 0)
                {
                    Selection.objects = Array.Empty<UnityEngine.Object>();
                }

                return;
            }

            UnityEngine.Object activeObject = selectedObjects[0];
            bool matchesSelection = Selection.objects.Length == selectedObjects.Count;
            if (matchesSelection)
            {
                for (int i = 0; i < selectedObjects.Count; i++)
                {
                    if (Selection.objects[i] != selectedObjects[i])
                    {
                        matchesSelection = false;
                        break;
                    }
                }
            }

            if (!matchesSelection)
            {
                Selection.objects = selectedObjects.ToArray();
            }

            if (Selection.activeObject != activeObject)
            {
                Selection.activeObject = activeObject;
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return item is HierarchyTreeItem hierarchyItem && hierarchyItem.IsGameObject;
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
            return item is HierarchyTreeItem hierarchyItem && hierarchyItem.IsGameObject && hierarchyItem.GameObject != null;
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
                    return item.IsGameObject;
                }
            }

            return false;
        }

        private void DrawEcsRow(Rect rowRect, HierarchyTreeItem item, bool selected)
        {
            Rect labelRect = rowRect;
            labelRect.xMin += GetContentIndent(item);
            labelRect.xMax -= 132f;

            GUIStyle labelStyle = item.Kind == HierarchyTreeItem.ItemKind.EcsRoot
                ? EditorStyles.boldLabel
                : selected ? CustomHierarchyStyles.SelectedEcsLabelStyle : CustomHierarchyStyles.EcsLabelStyle;
            EditorGUI.LabelField(labelRect, item.displayName, labelStyle);

            Rect statsRect = rowRect;
            statsRect.xMin = rowRect.xMax - 128f;
            string statsText = item.Kind == HierarchyTreeItem.ItemKind.EcsChunk
                ? $"{item.EntityCount}/{item.Capacity}"
                : item.Kind == HierarchyTreeItem.ItemKind.EcsEntityPage
                    ? item.RangeLength.ToString()
                : item.Kind == HierarchyTreeItem.ItemKind.EcsEntity
                    ? item.EntityId.ToString()
                : item.EntityCount.ToString();
            EditorGUI.LabelField(statsRect, statsText, EditorStyles.miniLabel);
        }

        private void AddEcsSummaryItem()
        {
            if (!(IEntityManager.Instance is EntityManager entityManager))
            {
                return;
            }

            List<TreeViewItem> archetypeChildren = new List<TreeViewItem>();
            int totalEntityCount = 0;
            int totalChunkCount = 0;

            for (int archetypeIndex = 0; archetypeIndex < entityManager.ArchetypeCount; archetypeIndex++)
            {
                ref Archetype archetype = ref entityManager.GetArchetype(archetypeIndex);
                List<TreeViewItem> chunkChildren = new List<TreeViewItem>();
                int archetypeEntityCount = 0;
                string archetypeSignature = archetype.GetDebugSignature();

                for (int chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
                {
                    ref Chunk chunk = ref archetype.GetChunk(chunkIndex);
                    if (chunk.EntityCount == 0)
                    {
                        continue;
                    }

                    archetypeEntityCount += chunk.EntityCount;
                    totalChunkCount += 1;
                    List<TreeViewItem> entityPageChildren = BuildEcsEntityPageChildren(archetypeIndex, chunkIndex, chunk.EntityCount);
                    chunkChildren.Add(new HierarchyTreeItem(
                        GetEcsChunkId(archetypeIndex, chunkIndex),
                        2,
                        $"Chunk {chunkIndex}",
                        HierarchyTreeItem.ItemKind.EcsChunk,
                        archetypeIndex,
                        chunkIndex,
                        chunk.EntityCount,
                        chunk.EntityCapacity));
                    chunkChildren[chunkChildren.Count - 1].children = entityPageChildren;
                }

                if (chunkChildren.Count == 0)
                {
                    continue;
                }

                totalEntityCount += archetypeEntityCount;
                archetypeChildren.Add(new HierarchyTreeItem(
                    GetEcsArchetypeId(archetypeIndex),
                    1,
                    $"Archetype {archetypeIndex} [{archetypeSignature}]",
                    HierarchyTreeItem.ItemKind.EcsArchetype,
                    archetypeIndex,
                    -1,
                    archetypeEntityCount,
                    chunkChildren.Count)
                {
                    children = chunkChildren,
                });
            }

            if (archetypeChildren.Count == 0)
            {
                return;
            }

            rootChildren.Add(new HierarchyTreeItem(
                EcsRootId,
                0,
                $"ECS ({entityManager.ArchetypeCount} archetypes, {totalChunkCount} chunks)",
                HierarchyTreeItem.ItemKind.EcsRoot,
                -1,
                -1,
                totalEntityCount,
                totalChunkCount)
            {
                children = archetypeChildren,
            });
        }

        private List<TreeViewItem> BuildEcsEntityPageChildren(int archetypeIndex, int chunkIndex, int entityCount)
        {
            if (entityCount <= 0)
            {
                return null;
            }

            List<TreeViewItem> pages = new List<TreeViewItem>((entityCount + EcsEntitiesPerPage - 1) / EcsEntitiesPerPage);
            for (int rangeStart = 0; rangeStart < entityCount; rangeStart += EcsEntitiesPerPage)
            {
                int rangeLength = Math.Min(EcsEntitiesPerPage, entityCount - rangeStart);
                int rangeEnd = rangeStart + rangeLength - 1;
                pages.Add(new HierarchyTreeItem(
                    GetEcsEntityPageId(archetypeIndex, chunkIndex, rangeStart),
                    3,
                    $"Entities {rangeStart}-{rangeEnd}",
                    HierarchyTreeItem.ItemKind.EcsEntityPage,
                    archetypeIndex,
                    chunkIndex,
                    rangeLength,
                    entityCount,
                    rangeStart,
                    rangeLength));

                if (IsExpanded(pages[pages.Count - 1].id))
                {
                    pages[pages.Count - 1].children = BuildEcsEntityChildren(archetypeIndex, chunkIndex, rangeStart, rangeLength);
                }
            }

            return pages;
        }

        private List<TreeViewItem> BuildEcsEntityChildren(int archetypeIndex, int chunkIndex, int rangeStart, int rangeLength)
        {
            if (!(IEntityManager.Instance is EntityManager entityManager))
            {
                return null;
            }

            ref Archetype archetype = ref entityManager.GetArchetype(archetypeIndex);
            ref Chunk chunk = ref archetype.GetChunk(chunkIndex);
            int clampedLength = Math.Min(rangeLength, Math.Max(0, chunk.EntityCount - rangeStart));
            if (clampedLength <= 0)
            {
                return null;
            }

            List<TreeViewItem> entityChildren = new List<TreeViewItem>(clampedLength);
            for (int i = 0; i < clampedLength; i++)
            {
                int slotIndex = rangeStart + i;
                int entityId = chunk.GetEntityIdAt(slotIndex);
                Entity entity = entityManager.GetEntity(entityId);
                entityChildren.Add(new HierarchyTreeItem(
                    GetEcsEntityId(archetypeIndex, chunkIndex, slotIndex),
                    4,
                    $"Entity {entity.Id}",
                    HierarchyTreeItem.ItemKind.EcsEntity,
                    archetypeIndex,
                    chunkIndex,
                    1,
                    chunk.EntityCount,
                    slotIndex,
                    1,
                    entity.Id));
            }

            return entityChildren;
        }

        public bool TryGetSelectedHierarchyItem(out HierarchyTreeItem item)
        {
            item = null;
            IList<int> selection = GetSelection();
            if (selection == null || selection.Count == 0)
            {
                return false;
            }

            return TryGetHierarchyItem(selection[0], out item);
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
            bool accepted = HandleObjectsDropped(draggedObjects, targetId, args.insertAtIndex, args.performDrop);
            return accepted ? DragAndDropVisualMode.Move : DragAndDropVisualMode.Rejected;
        }

        public bool ContainsObject(GameObject gameObject)
        {
            return gameObject != null && FindItem(gameObject.GetInstanceID(), rootItem) != null;
        }

        public bool TryGetHierarchyItem(int id, out HierarchyTreeItem item)
        {
            item = FindItem(id, rootItem) as HierarchyTreeItem;
            return item != null;
        }

        public void SelectEcsEntity(int archetypeIndex, int chunkIndex, int slotIndex)
        {
            int pageStart = Mathf.Max(0, slotIndex / EcsEntitiesPerPage * EcsEntitiesPerPage);
            int archetypeId = GetEcsArchetypeId(archetypeIndex);
            int chunkId = GetEcsChunkId(archetypeIndex, chunkIndex);
            int pageId = GetEcsEntityPageId(archetypeIndex, chunkIndex, pageStart);
            int entityId = GetEcsEntityId(archetypeIndex, chunkIndex, slotIndex);

            SetExpanded(EcsRootId, true);
            SetExpanded(archetypeId, true);
            SetExpanded(chunkId, true);
            SetExpanded(pageId, true);
            Reload();

            SetSelection(new List<int> { entityId }, TreeViewSelectionOptions.RevealAndFrame);
        }

        public void SelectEcsEntityPage(int archetypeIndex, int chunkIndex, int slotIndex)
        {
            int pageStart = Mathf.Max(0, slotIndex / EcsEntitiesPerPage * EcsEntitiesPerPage);
            int archetypeId = GetEcsArchetypeId(archetypeIndex);
            int chunkId = GetEcsChunkId(archetypeIndex, chunkIndex);
            int pageId = GetEcsEntityPageId(archetypeIndex, chunkIndex, pageStart);

            SetExpanded(EcsRootId, true);
            SetExpanded(archetypeId, true);
            SetExpanded(chunkId, true);
            Reload();

            SetSelection(new List<int> { pageId }, TreeViewSelectionOptions.RevealAndFrame);
        }

        private static int GetEcsArchetypeId(int archetypeIndex)
        {
            return EcsArchetypeBaseId - archetypeIndex;
        }

        private static int GetEcsChunkId(int archetypeIndex, int chunkIndex)
        {
            int chunkKey = archetypeIndex * MaxChunksPerArchetype + chunkIndex;
            return EcsChunkBaseId - chunkKey;
        }

        private static int GetEcsEntityPageId(int archetypeIndex, int chunkIndex, int rangeStart)
        {
            int pageIndex = rangeStart / EcsEntitiesPerPage;
            int pageKey = (archetypeIndex * MaxChunksPerArchetype + chunkIndex) * MaxPagesPerChunk + pageIndex;
            return EcsEntityPageBaseId - pageKey;
        }

        private static int GetEcsEntityId(int archetypeIndex, int chunkIndex, int slotIndex)
        {
            int entityKey = (archetypeIndex * MaxChunksPerArchetype + chunkIndex) * MaxEntitiesPerChunk + slotIndex;
            return EcsEntityBaseId - entityKey;
        }

        public void BeginRenameById(int id)
        {
            if (TryGetHierarchyItem(id, out HierarchyTreeItem item) && !item.IsSceneHeader)
            {
                BeginRename(item);
            }
        }

        private void AddSceneItem(Scene scene, string fallbackName)
        {
            if (!scene.IsValid())
            {
                return;
            }

            string sceneDisplayName = string.IsNullOrEmpty(scene.name) ? fallbackName : scene.name;
            List<TreeViewItem> sceneChildren = BuildSceneChildren(scene);
            int visibleObjectCount = CountVisibleSceneObjects(sceneChildren);
            string headerName = visibleObjectCount > 0 || string.IsNullOrEmpty(searchString)
                ? $"{sceneDisplayName} ({visibleObjectCount})"
                : sceneDisplayName;
            int sceneHeaderId = SceneHeaderBaseId - scene.handle;

            if (scene == SceneManager.GetActiveScene())
            {
                SetExpanded(sceneHeaderId, true);
            }

            HierarchyTreeItem sceneItem = new HierarchyTreeItem(sceneHeaderId, 0, headerName, scene, visibleObjectCount)
            {
                children = sceneChildren,
            };

            if (sceneItem.children.Count > 0 || string.IsNullOrEmpty(searchString))
            {
                rootChildren.Add(sceneItem);
            }
        }

        private List<TreeViewItem> BuildSceneChildren(Scene scene)
        {
            List<TreeViewItem> sceneChildren = new List<TreeViewItem>();
            GameObject[] roots = GetSceneRootGameObjects(scene);
            for (int i = 0; i < roots.Length; i++)
            {
                HierarchyTreeItem objectItem = BuildGameObjectItem(roots[i], 1);
                if (objectItem != null)
                {
                    sceneChildren.Add(objectItem);
                }
            }

            return sceneChildren;
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
            for (int i = 0; i < transform.childCount; i++)
            {
                HierarchyTreeItem childItem = BuildGameObjectItem(transform.GetChild(i).gameObject, depth + 1);
                if (childItem == null)
                {
                    continue;
                }

                childItems ??= new List<TreeViewItem>();
                childItems.Add(childItem);
            }

            bool includeBySelf = nameMatch && visibleByState && passesAdditional;
            bool includeByChildren = childItems != null && childItems.Count > 0;
            if (!includeBySelf && !includeByChildren)
            {
                return null;
            }

            return new HierarchyTreeItem(gameObject.GetInstanceID(), depth, gameObject.name, gameObject)
            {
                children = childItems,
                icon = EditorGUIUtility.ObjectContent(gameObject, typeof(GameObject)).image as Texture2D,
            };
        }

        private static int CountVisibleSceneObjects(List<TreeViewItem> items)
        {
            if (items == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is HierarchyTreeItem item)
                {
                    count += 1;
                    count += CountVisibleSceneObjects(item.children);
                }
            }

            return count;
        }

        private static GameObject[] GetSceneRootGameObjects(Scene scene)
        {
            if (!scene.IsValid())
            {
                return Array.Empty<GameObject>();
            }

            if (scene.isLoaded)
            {
                return scene.GetRootGameObjects();
            }

            List<GameObject> roots = new List<GameObject>();
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject gameObject = allObjects[i];
                if (gameObject == null || gameObject.transform.parent != null)
                {
                    continue;
                }

                if (gameObject.hideFlags != HideFlags.None || gameObject.scene != scene)
                {
                    continue;
                }

                roots.Add(gameObject);
            }

            return roots.ToArray();
        }

        private static bool TryGetDontDestroyOnLoadScene(out Scene scene)
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < allObjects.Length; i++)
            {
                GameObject gameObject = allObjects[i];
                if (gameObject == null || gameObject.transform.parent != null)
                {
                    continue;
                }

                Scene candidateScene = gameObject.scene;
                if (!candidateScene.IsValid())
                {
                    continue;
                }

                if (string.Equals(candidateScene.name, "DontDestroyOnLoad", StringComparison.Ordinal))
                {
                    scene = candidateScene;
                    return true;
                }
            }

            scene = default;
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
                if (TryGetHierarchyItem(draggedItemIds[i], out HierarchyTreeItem item) && !item.IsSceneHeader && item.GameObject != null)
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
                if (references[i] is GameObject gameObject)
                {
                    draggedObjects.Add(gameObject);
                }
            }

            return draggedObjects;
        }
    }
}
