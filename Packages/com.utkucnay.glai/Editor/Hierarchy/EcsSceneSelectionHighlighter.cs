using Glai.ECS;
using Glai.ECS.Core;
using Glai.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Glai.Core.Editor
{
    [InitializeOnLoad]
    internal static class EcsSceneSelectionHighlighter
    {
        private const float MarkerSize = 2.5f;
        private const float PickSize = 1f;
        private static SelectionKey? activeSelection;

        static EcsSceneSelectionHighlighter()
        {
            SceneView.duringSceneGui += DrawSelectionMarker;
            Selection.selectionChanged += HandleSelectionChanged;
        }

        public static void Sync(EcsSelectionProxy proxy)
        {
            if (proxy == null || proxy.Kind != HierarchyTreeItem.ItemKind.EcsEntity)
            {
                Clear();
                return;
            }

            if (!(IEntityManager.Instance is EntityManager entityManager) ||
                !TryGetEntityWorldPosition(proxy, entityManager, out Vector3 worldPosition, out _))
            {
                Clear();
                return;
            }

            SelectionKey nextSelection = new SelectionKey(proxy.ArchetypeIndex, proxy.ChunkIndex, proxy.RangeStart, proxy.EntityId);
            activeSelection = nextSelection;

            SceneView.RepaintAll();
        }

        public static void ShowSelection()
        {
            if (!activeSelection.HasValue || !(IEntityManager.Instance is EntityManager entityManager))
            {
                return;
            }

            if (!TryGetEntityWorldPosition(activeSelection.Value, entityManager, out Vector3 worldPosition, out _))
            {
                Clear();
                return;
            }

            FrameSelection(worldPosition);
        }

        public static void Clear()
        {
            if (!activeSelection.HasValue)
            {
                return;
            }

            activeSelection = null;
            SceneView.RepaintAll();
        }

        public static bool TryGetEntityWorldPosition(EcsSelectionProxy proxy, EntityManager entityManager, out Vector3 worldPosition, out string error)
        {
            worldPosition = default;

            if (!TryGetSelectedEntity(proxy.ArchetypeIndex, proxy.ChunkIndex, proxy.RangeStart, proxy.EntityId, entityManager, out Entity entity, out Archetype archetype, out error))
            {
                return false;
            }

            return TryGetWorldPosition(entityManager, entity, ref archetype, out worldPosition, out error);
        }

        private static void HandleSelectionChanged()
        {
            if (!(Selection.activeObject is EcsSelectionProxy))
            {
                Clear();
            }
        }

        private static void DrawSelectionMarker(SceneView sceneView)
        {
            HandleScenePicking();

            if (!activeSelection.HasValue || !(IEntityManager.Instance is EntityManager entityManager))
            {
                return;
            }

            if (!TryGetEntityWorldPosition(activeSelection.Value, entityManager, out Vector3 worldPosition, out _))
            {
                Clear();
                return;
            }

            using (new Handles.DrawingScope(new Color(0.2f, 0.85f, 1f, 0.95f)))
            {
                float handleSize = HandleUtility.GetHandleSize(worldPosition);
                float markerSize = Mathf.Max(0.75f, handleSize * 0.35f);
                Handles.DrawWireCube(worldPosition, Vector3.one * markerSize * MarkerSize);
                Handles.DrawWireDisc(worldPosition, Vector3.up, markerSize * MarkerSize * 0.75f);
                Handles.DrawLine(worldPosition - Vector3.right * markerSize * MarkerSize, worldPosition + Vector3.right * markerSize * MarkerSize);
                Handles.DrawLine(worldPosition - Vector3.forward * markerSize * MarkerSize, worldPosition + Vector3.forward * markerSize * MarkerSize);
            }
        }

        private static void HandleScenePicking()
        {
            Event currentEvent = Event.current;
            if (currentEvent == null ||
                currentEvent.type != EventType.MouseDown ||
                currentEvent.button != 0 ||
                currentEvent.alt ||
                currentEvent.control ||
                currentEvent.command)
            {
                return;
            }

            if (!(IEntityManager.Instance is EntityManager entityManager) ||
                !TryPickEntity(currentEvent.mousePosition, entityManager, out SelectionKey selection))
            {
                return;
            }

            EcsSelectionProxy proxy = EcsSelectionProxy.GetOrCreate();
            proxy.SelectEntity(selection.ArchetypeIndex, selection.ChunkIndex, selection.RangeStart, selection.EntityId);
            Selection.activeObject = proxy;
            currentEvent.Use();
        }

        private static bool TryPickEntity(Vector2 mousePosition, EntityManager entityManager, out SelectionKey selection)
        {
            selection = default;

            Ray worldRay = HandleUtility.GUIPointToWorldRay(mousePosition);
            float bestDistance = float.PositiveInfinity;
            bool found = false;

            for (int archetypeIndex = 0; archetypeIndex < entityManager.ArchetypeCount; archetypeIndex++)
            {
                ref Archetype archetype = ref entityManager.GetArchetype(archetypeIndex);
                if (!archetype.HasAll<MeshRendererComponent, PackedTransformComponent>())
                {
                    continue;
                }

                for (int chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
                {
                    ref Chunk chunk = ref archetype.GetChunk(chunkIndex);
                    for (int slotIndex = 0; slotIndex < chunk.EntityCount; slotIndex++)
                    {
                        int entityId = chunk.GetEntityIdAt(slotIndex);

                        Entity entity;
                        try
                        {
                            entity = entityManager.GetEntity(entityId);
                        }
                        catch
                        {
                            continue;
                        }

                        if (!entityManager.IsValid(entity))
                        {
                            continue;
                        }

                        PackedTransformComponent transform = entityManager.GetComponent<PackedTransformComponent>(entity);
                        Vector3 worldPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                        Bounds bounds = new Bounds(worldPosition, Vector3.one * PickSize);
                        if (!bounds.IntersectRay(worldRay, out float hitDistance) || hitDistance >= bestDistance)
                        {
                            continue;
                        }

                        bestDistance = hitDistance;
                        selection = new SelectionKey(archetypeIndex, chunkIndex, slotIndex, entityId);
                        found = true;
                    }
                }
            }

            return found;
        }

        private static bool TryGetEntityWorldPosition(SelectionKey selection, EntityManager entityManager, out Vector3 worldPosition, out string error)
        {
            worldPosition = default;

            if (!TryGetSelectedEntity(selection.ArchetypeIndex, selection.ChunkIndex, selection.RangeStart, selection.EntityId, entityManager, out Entity entity, out Archetype archetype, out error))
            {
                return false;
            }

            return TryGetWorldPosition(entityManager, entity, ref archetype, out worldPosition, out error);
        }

        private static bool TryGetWorldPosition(EntityManager entityManager, Entity entity, ref Archetype archetype, out Vector3 worldPosition, out string error)
        {
            worldPosition = default;

            if (archetype.HasComponent<PackedTransformComponent>())
            {
                PackedTransformComponent packedTransform = entityManager.GetComponent<PackedTransformComponent>(entity);
                worldPosition = new Vector3(packedTransform.position.x, packedTransform.position.y, packedTransform.position.z);
                error = null;
                return true;
            }

            if (archetype.HasComponent<TransformComponent>())
            {
                TransformComponent transform = entityManager.GetComponent<TransformComponent>(entity);
                worldPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                error = null;
                return true;
            }

            error = "Entity has no supported transform component, so it cannot be shown in the Scene view.";
            return false;
        }

        private static bool TryGetSelectedEntity(int archetypeIndex, int chunkIndex, int rangeStart, int entityId, EntityManager entityManager, out Entity entity, out Archetype archetype, out string error)
        {
            entity = default;
            archetype = default;

            if (archetypeIndex < 0 || archetypeIndex >= entityManager.ArchetypeCount)
            {
                error = "Selected entity no longer exists.";
                return false;
            }

            archetype = entityManager.GetArchetype(archetypeIndex);
            if (chunkIndex < 0 || chunkIndex >= archetype.ChunkCount)
            {
                error = "Selected entity no longer exists.";
                return false;
            }

            Chunk chunk = archetype.GetChunk(chunkIndex);
            if (rangeStart < 0 || rangeStart >= chunk.EntityCount)
            {
                error = "Selected entity no longer exists.";
                return false;
            }

            if (chunk.GetEntityIdAt(rangeStart) != entityId)
            {
                error = "Selected entity no longer exists.";
                return false;
            }

            try
            {
                entity = entityManager.GetEntity(entityId);
            }
            catch
            {
                error = "Selected entity no longer exists.";
                return false;
            }

            if (!entityManager.IsValid(entity))
            {
                error = "Selected entity no longer exists.";
                return false;
            }

            error = null;
            return true;
        }

        private static void FrameSelection(Vector3 worldPosition)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return;
            }

            sceneView.Focus();
            sceneView.Frame(new Bounds(worldPosition, Vector3.one * 12f), false);
        }
        private readonly struct SelectionKey
        {
            public readonly int ArchetypeIndex;
            public readonly int ChunkIndex;
            public readonly int RangeStart;
            public readonly int EntityId;

            public SelectionKey(int archetypeIndex, int chunkIndex, int rangeStart, int entityId)
            {
                ArchetypeIndex = archetypeIndex;
                ChunkIndex = chunkIndex;
                RangeStart = rangeStart;
                EntityId = entityId;
            }
        }
    }
}
