using System;
using UnityEditor;
using UnityEngine;

namespace Glai.Core.Editor
{
    internal sealed class EcsSelectionProxy : ScriptableObject
    {
        public const int ChunkPageSize = 10;

        private static EcsSelectionProxy instance;

        public HierarchyTreeItem.ItemKind Kind;
        public int ArchetypeIndex;
        public int ChunkIndex;
        public int RangeStart;
        public int RangeLength;
        public int EntityId;
        public int EntityCount;
        public int Capacity;
        public int EntityPageStart;

        public static EcsSelectionProxy GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = CreateInstance<EcsSelectionProxy>();
            instance.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
            instance.name = "ECS Selection";
            return instance;
        }

        public void Apply(HierarchyTreeItem item)
        {
            Kind = item.Kind;
            ArchetypeIndex = item.ArchetypeIndex;
            ChunkIndex = item.ChunkIndex;
            RangeStart = item.RangeStart;
            RangeLength = item.RangeLength;
            EntityId = item.EntityId;
            EntityCount = item.EntityCount;
            Capacity = item.Capacity;
            EntityPageStart = item.Kind == HierarchyTreeItem.ItemKind.EcsEntity
                ? GetChunkPageStart(item.RangeStart)
                : item.Kind == HierarchyTreeItem.ItemKind.EcsEntityPage
                    ? Math.Max(0, item.RangeStart)
                    : 0;

            NotifyChanged();
        }

        public void SetChunkPageStart(int pageStart)
        {
            EntityPageStart = Math.Max(0, pageStart);
            NotifyChanged();
        }

        public void ShowChunk()
        {
            Kind = HierarchyTreeItem.ItemKind.EcsChunk;
            RangeStart = -1;
            RangeLength = 0;
            EntityId = -1;
            NotifyChanged();
        }

        public void SelectEntity(int slotIndex, int entityId)
        {
            Kind = HierarchyTreeItem.ItemKind.EcsEntity;
            RangeStart = slotIndex;
            RangeLength = 1;
            EntityId = entityId;
            EntityPageStart = GetChunkPageStart(slotIndex);
            NotifyChanged();
        }

        public void SelectEntity(int archetypeIndex, int chunkIndex, int slotIndex, int entityId)
        {
            Kind = HierarchyTreeItem.ItemKind.EcsEntity;
            ArchetypeIndex = archetypeIndex;
            ChunkIndex = chunkIndex;
            RangeStart = slotIndex;
            RangeLength = 1;
            EntityId = entityId;
            EntityPageStart = GetChunkPageStart(slotIndex);
            NotifyChanged();
        }

        public void SelectEntityWithoutChangingKind(int slotIndex, int entityId)
        {
            RangeStart = slotIndex;
            RangeLength = 1;
            EntityId = entityId;
            EntityPageStart = GetChunkPageStart(slotIndex);
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            EcsSceneSelectionHighlighter.Sync(this);
            EditorUtility.SetDirty(this);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static int GetChunkPageStart(int slotIndex)
        {
            if (slotIndex < 0)
            {
                return 0;
            }

            return slotIndex / ChunkPageSize * ChunkPageSize;
        }
    }
}
