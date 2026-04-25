using System;
using System.Reflection;
using Glai.ECS;
using Glai.ECS.Core;
using UnityEditor;
using UnityEngine;

namespace Glai.Core.Editor
{
    [CustomEditor(typeof(EcsSelectionProxy))]
    internal sealed class EcsSelectionProxyEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        public override void OnInspectorGUI()
        {
            EcsSelectionProxy proxy = (EcsSelectionProxy)target;

            EditorGUILayout.LabelField("ECS Details", EditorStyles.boldLabel);

            if (!(IEntityManager.Instance is EntityManager entityManager))
            {
                EditorGUILayout.HelpBox("EntityManager unavailable.", MessageType.Info);
                return;
            }

            switch (proxy.Kind)
            {
                case HierarchyTreeItem.ItemKind.EcsRoot:
                    DrawRoot(entityManager);
                    break;
                case HierarchyTreeItem.ItemKind.EcsArchetype:
                    DrawArchetype(entityManager, proxy);
                    break;
                case HierarchyTreeItem.ItemKind.EcsChunk:
                    DrawChunk(entityManager, proxy);
                    break;
                case HierarchyTreeItem.ItemKind.EcsEntityPage:
                    DrawEntityPage(entityManager, proxy);
                    break;
                case HierarchyTreeItem.ItemKind.EcsEntity:
                    DrawEntity(entityManager, proxy);
                    break;
                default:
                    EditorGUILayout.HelpBox("Unsupported ECS selection.", MessageType.Info);
                    break;
            }
        }

        private static void DrawRoot(EntityManager entityManager)
        {
            int totalEntityCount = 0;
            int totalChunkCount = 0;

            for (int archetypeIndex = 0; archetypeIndex < entityManager.ArchetypeCount; archetypeIndex++)
            {
                ref Archetype archetype = ref entityManager.GetArchetype(archetypeIndex);
                for (int chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
                {
                    ref Chunk chunk = ref archetype.GetChunk(chunkIndex);
                    if (chunk.EntityCount == 0)
                    {
                        continue;
                    }

                    totalChunkCount += 1;
                    totalEntityCount += chunk.EntityCount;
                }
            }

            EditorGUILayout.LabelField("Archetypes", entityManager.ArchetypeCount.ToString());
            EditorGUILayout.LabelField("Chunks", totalChunkCount.ToString());
            EditorGUILayout.LabelField("Entities", totalEntityCount.ToString());
        }

        private static void DrawArchetype(EntityManager entityManager, EcsSelectionProxy proxy)
        {
            if (!TryGetArchetype(entityManager, proxy.ArchetypeIndex, out Archetype archetype))
            {
                DrawStaleSelection();
                return;
            }

            int nonEmptyChunkCount = 0;
            int entityCount = 0;
            for (int chunkIndex = 0; chunkIndex < archetype.ChunkCount; chunkIndex++)
            {
                ref Chunk chunk = ref archetype.GetChunk(chunkIndex);
                if (chunk.EntityCount == 0)
                {
                    continue;
                }

                nonEmptyChunkCount += 1;
                entityCount += chunk.EntityCount;
            }

            EditorGUILayout.LabelField("Archetype", proxy.ArchetypeIndex.ToString());
            EditorGUILayout.LabelField("Signature", archetype.GetDebugSignature(), EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Chunks", nonEmptyChunkCount.ToString());
            EditorGUILayout.LabelField("Entities", entityCount.ToString());
        }

        private static void DrawChunk(EntityManager entityManager, EcsSelectionProxy proxy)
        {
            if (!TryGetChunk(entityManager, proxy.ArchetypeIndex, proxy.ChunkIndex, out Chunk chunk))
            {
                DrawStaleSelection();
                return;
            }

            EditorGUILayout.LabelField("Archetype", proxy.ArchetypeIndex.ToString());
            EditorGUILayout.LabelField("Chunk", proxy.ChunkIndex.ToString());
            EditorGUILayout.LabelField("Entities", chunk.EntityCount.ToString());
            EditorGUILayout.LabelField("Capacity", chunk.EntityCapacity.ToString());
            EditorGUILayout.LabelField("Occupancy", $"{chunk.EntityCount}/{chunk.EntityCapacity}");

            DrawChunkEntities(entityManager, proxy, ref chunk);
        }

        private static void DrawEntityPage(EntityManager entityManager, EcsSelectionProxy proxy)
        {
            if (!TryGetChunk(entityManager, proxy.ArchetypeIndex, proxy.ChunkIndex, out Chunk chunk))
            {
                DrawStaleSelection();
                return;
            }

            if (proxy.RangeStart < 0 || proxy.RangeStart >= chunk.EntityCount)
            {
                DrawStaleSelection();
                return;
            }

            int rangeLength = Mathf.Min(proxy.RangeLength, chunk.EntityCount - proxy.RangeStart);
            int rangeEnd = proxy.RangeStart + rangeLength - 1;

            EditorGUILayout.LabelField("Archetype", proxy.ArchetypeIndex.ToString());
            EditorGUILayout.LabelField("Chunk", proxy.ChunkIndex.ToString());
            EditorGUILayout.LabelField("Range", $"{proxy.RangeStart}-{rangeEnd}");
            EditorGUILayout.LabelField("Entities", rangeLength.ToString());

            DrawChunkEntities(entityManager, proxy, ref chunk);
        }

        private static void DrawEntity(EntityManager entityManager, EcsSelectionProxy proxy)
        {
            if (!TryGetArchetype(entityManager, proxy.ArchetypeIndex, out Archetype archetype) ||
                proxy.ChunkIndex < 0 ||
                proxy.ChunkIndex >= archetype.ChunkCount)
            {
                DrawStaleSelection();
                return;
            }

            ref Chunk chunk = ref archetype.GetChunk(proxy.ChunkIndex);
            if (proxy.RangeStart < 0 || proxy.RangeStart >= chunk.EntityCount)
            {
                DrawStaleSelection();
                return;
            }

            int entityId = chunk.GetEntityIdAt(proxy.RangeStart);
            if (entityId != proxy.EntityId)
            {
                DrawStaleSelection();
                return;
            }

            Entity entity;
            try
            {
                entity = entityManager.GetEntity(entityId);
            }
            catch
            {
                DrawStaleSelection();
                return;
            }

            if (!entityManager.IsValid(entity))
            {
                DrawStaleSelection();
                return;
            }

            EditorGUILayout.LabelField("Entity Id", entity.Id.ToString());
            EditorGUILayout.LabelField("Archetype", proxy.ArchetypeIndex.ToString());
            EditorGUILayout.LabelField("Chunk", proxy.ChunkIndex.ToString());
            EditorGUILayout.LabelField("Chunk Slot", proxy.RangeStart.ToString());

            if (EcsSceneSelectionHighlighter.TryGetEntityWorldPosition(proxy, entityManager, out Vector3 worldPosition, out string highlightError))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Vector3Field("World Position", worldPosition);
                }

                if (GUILayout.Button("Show", GUILayout.Width(64f)))
                {
                    EcsSceneSelectionHighlighter.ShowSelection();
                }
            }
            else if (!string.IsNullOrEmpty(highlightError))
            {
                EditorGUILayout.HelpBox(highlightError, MessageType.Info);
            }

            DrawEntityNavigation(proxy, ref chunk);

            if (GUILayout.Button("Back To Chunk"))
            {
                proxy.ShowChunk();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.Space();
            DrawComponentSnapshots(ref archetype, ref chunk, proxy.RangeStart);
        }

        private static void DrawEntityNavigation(EcsSelectionProxy proxy, ref Chunk chunk)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(proxy.RangeStart <= 0))
            {
                if (GUILayout.Button("Previous Entity"))
                {
                    int slotIndex = proxy.RangeStart - 1;
                    proxy.SelectEntityWithoutChangingKind(slotIndex, chunk.GetEntityIdAt(slotIndex));
                    GUIUtility.ExitGUI();
                }
            }

            using (new EditorGUI.DisabledScope(proxy.RangeStart >= chunk.EntityCount - 1))
            {
                if (GUILayout.Button("Next Entity"))
                {
                    int slotIndex = proxy.RangeStart + 1;
                    proxy.SelectEntityWithoutChangingKind(slotIndex, chunk.GetEntityIdAt(slotIndex));
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawChunkEntities(EntityManager entityManager, EcsSelectionProxy proxy, ref Chunk chunk)
        {
            EditorGUILayout.Space();

            int clampedPageStart = Mathf.Clamp(proxy.EntityPageStart, 0, Mathf.Max(0, chunk.EntityCount - 1));
            clampedPageStart -= clampedPageStart % EcsSelectionProxy.ChunkPageSize;
            if (clampedPageStart != proxy.EntityPageStart)
            {
                proxy.SetChunkPageStart(clampedPageStart);
            }

            int rangeEndExclusive = Mathf.Min(clampedPageStart + EcsSelectionProxy.ChunkPageSize, chunk.EntityCount);
            int rangeEndInclusive = rangeEndExclusive - 1;

            EditorGUILayout.LabelField("Page", chunk.EntityCount == 0
                ? "No entities"
                : $"Entities {clampedPageStart}-{rangeEndInclusive} of {chunk.EntityCount - 1}");

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(clampedPageStart <= 0))
            {
                if (GUILayout.Button("Previous"))
                {
                    proxy.SetChunkPageStart(Mathf.Max(0, clampedPageStart - EcsSelectionProxy.ChunkPageSize));
                    GUIUtility.ExitGUI();
                }
            }

            using (new EditorGUI.DisabledScope(rangeEndExclusive >= chunk.EntityCount))
            {
                if (GUILayout.Button("Next"))
                {
                    proxy.SetChunkPageStart(clampedPageStart + EcsSelectionProxy.ChunkPageSize);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (chunk.EntityCount == 0)
            {
                return;
            }

            EditorGUILayout.Space();
            for (int slotIndex = clampedPageStart; slotIndex < rangeEndExclusive; slotIndex++)
            {
                int entityId = chunk.GetEntityIdAt(slotIndex);

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Slot {slotIndex}", GUILayout.Width(56f));
                EditorGUILayout.LabelField($"Entity {entityId}", GUILayout.Width(72f));
                if (GUILayout.Button("Inspect", GUILayout.Width(64f)))
                {
                    proxy.SelectEntity(slotIndex, entityId);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void DrawComponentSnapshots(ref Archetype archetype, ref Chunk chunk, int slotIndex)
        {
            EcsComponentSnapshot[] snapshots = EcsInspectorBridge.GetComponentSnapshots(ref archetype, ref chunk, slotIndex);
            for (int i = 0; i < snapshots.Length; i++)
            {
                DrawComponentSnapshot(snapshots[i]);
            }
        }

        private static void DrawComponentSnapshot(EcsComponentSnapshot snapshot)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(snapshot.TypeName, EditorStyles.boldLabel);

            if (snapshot.IsTag)
            {
                EditorGUILayout.LabelField("Kind", "Tag");
                EditorGUILayout.EndVertical();
                return;
            }

            if (snapshot.IsBuffer)
            {
                EditorGUILayout.LabelField("Kind", "Buffer");
                EditorGUILayout.LabelField("Value", FormatValue(snapshot.BoxedValue));
                EditorGUILayout.EndVertical();
                return;
            }

            if (snapshot.BoxedValue == null)
            {
                EditorGUILayout.LabelField("Value", "Unavailable");
                EditorGUILayout.EndVertical();
                return;
            }

            Type valueType = snapshot.BoxedValue.GetType();
            DrawMemberSection("Fields", valueType.GetFields(BindingFlags.Instance | BindingFlags.Public), snapshot.BoxedValue);
            DrawPropertySection(valueType, snapshot.BoxedValue);
            EditorGUILayout.EndVertical();
        }

        private static void DrawMemberSection(string header, FieldInfo[] fields, object instance)
        {
            if (fields.Length == 0)
            {
                return;
            }

            EditorGUILayout.LabelField(header, EditorStyles.miniBoldLabel);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                object value = field.GetValue(instance);
                EditorGUILayout.LabelField(field.Name, FormatValue(value), EditorStyles.wordWrappedLabel);
            }
        }

        private static void DrawPropertySection(Type valueType, object instance)
        {
            PropertyInfo[] properties = valueType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            bool headerDrawn = false;

            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                if (!property.CanRead || property.GetIndexParameters().Length > 0)
                {
                    continue;
                }

                if (!headerDrawn)
                {
                    EditorGUILayout.LabelField("Properties", EditorStyles.miniBoldLabel);
                    headerDrawn = true;
                }

                string valueText;
                try
                {
                    object value = property.GetValue(instance, null);
                    valueText = FormatValue(value);
                }
                catch (Exception ex)
                {
                    valueText = $"<error: {ex.GetType().Name}>";
                }

                EditorGUILayout.LabelField(property.Name, valueText, EditorStyles.wordWrappedLabel);
            }
        }

        private static string FormatValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            return value switch
            {
                string text => text,
                bool boolValue => boolValue ? "true" : "false",
                IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        private static bool TryGetArchetype(EntityManager entityManager, int archetypeIndex, out Archetype archetype)
        {
            if (archetypeIndex < 0 || archetypeIndex >= entityManager.ArchetypeCount)
            {
                archetype = default;
                return false;
            }

            archetype = entityManager.GetArchetype(archetypeIndex);
            return true;
        }

        private static bool TryGetChunk(EntityManager entityManager, int archetypeIndex, int chunkIndex, out Chunk chunk)
        {
            if (!TryGetArchetype(entityManager, archetypeIndex, out Archetype archetype) ||
                chunkIndex < 0 ||
                chunkIndex >= archetype.ChunkCount)
            {
                chunk = default;
                return false;
            }

            chunk = archetype.GetChunk(chunkIndex);
            return true;
        }

        private static void DrawStaleSelection()
        {
            EditorGUILayout.HelpBox("Selection no longer exists.", MessageType.Info);
        }
    }
}
