using System;
using System.Collections.Generic;
using Glai.Collection;
using Glai.ECS.Core;
using Glai.Module;
using UnityEngine;
using UnityEngine.Scripting;

namespace Glai.ECS
{
    [Preserve]
    internal static class EntityManagerModuleRegistration
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Register()
        {
            Glai.Module.RuntimeModuleCatalog.Register<EntityManager>();
        }
    }

    [Preserve]
    public partial class EntityManager : ModuleBase, IEntityManager
    {
        const int MaxArchetypeCount = 128;
        const int MaxEntityCount = 300_000;

        FixedList<Archetype> archeTypes;
        FixedList<EntityRecord> entityRecords;
        FixedList<Entity> entities;
        FixedStack<int> recycledEntityIds;

        ECSMemoryState ecsMemoryState;
        public ECSMemoryState ECSMemoryState => ecsMemoryState;
        public int ArchetypeCount => archeTypes.Count;

        public override void Initialize()
        {
            IEntityManager.Instance = this;

            ecsMemoryState = new ECSMemoryState();
            archeTypes = new FixedList<Archetype>(MaxArchetypeCount, ecsMemoryState.persistHandle, ecsMemoryState);
            entityRecords = new FixedList<EntityRecord>(MaxEntityCount, ecsMemoryState.persistHandle, ecsMemoryState);
            entities = new FixedList<Entity>(MaxEntityCount, ecsMemoryState.persistHandle, ecsMemoryState);
            recycledEntityIds = new FixedStack<int>(MaxEntityCount, ecsMemoryState.persistHandle, ecsMemoryState);
        }

        public override void Dispose()
        {
            if (Disposed) return;

            archeTypes.Dispose(ecsMemoryState);
            entityRecords.Dispose(ecsMemoryState);
            entities.Dispose(ecsMemoryState);
            recycledEntityIds.Dispose(ecsMemoryState);
            ecsMemoryState.Dispose();
            IEntityManager.Instance = null;
            base.Dispose();
        }

        private int AddArchetype(ref ArchetypeData data)
        {
            archeTypes.Add(new Archetype(data, ecsMemoryState.persistHandle, ecsMemoryState));
            return archeTypes.Count - 1;
        }

        public int CreateArchetype(ReadOnlySpan<ArchetypeType> types)
        {
            if (types.Length == 0)
            {
                throw new InvalidOperationException("Archetype must contain at least one type.");
            }

            Span<int> componentTypeIds = stackalloc int[types.Length];
            Span<int> componentSizes = stackalloc int[types.Length];
            ArchetypeData archetypeData = new ArchetypeData(componentTypeIds, componentSizes);

            for (int i = 0; i < types.Length; i++)
            {
                archetypeData.AddType(types[i].TypeId, types[i].Size);
            }

            return AddArchetype(ref archetypeData);
        }

        private static int[] CopyAndSortTypeIds(FixedList<int> typeIds)
        {
            int[] copy = new int[typeIds.Count];
            for (int i = 0; i < typeIds.Count; i++)
            {
                copy[i] = typeIds[i];
            }

            Array.Sort(copy);
            return copy;
        }

        private static int[] CopyTypeIds(ReadOnlySpan<int> typeIds)
        {
            int[] copy = new int[typeIds.Length];
            for (int i = 0; i < typeIds.Length; i++)
            {
                copy[i] = typeIds[i];
            }

            return copy;
        }

        public Entity CreateEntity(int archetypeIndex)
        {
            if (archetypeIndex < 0 || archetypeIndex >= archeTypes.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(archetypeIndex), $"Invalid archetype index {archetypeIndex}.");
            }

            bool canRecycleEntityId = recycledEntityIds.Count > 0;
            if (!canRecycleEntityId && (entityRecords.Count >= entityRecords.Capacity || entities.Count >= entities.Capacity))
            {
                throw new InvalidOperationException("Entity storage is full.");
            }

            int entityId;
            Entity entity;

            if (canRecycleEntityId)
            {
                entityId = recycledEntityIds.Pop();
                entity = entities[entityId];
            }
            else
            {
                entityId = entities.Count;
                entity = new Entity(entityId);
                entities.Add(entity);
                entityRecords.Add(default);
            }

            ref var archetype = ref archeTypes.Get(archetypeIndex);
            var entityRecord = archetype.AddEntity(ecsMemoryState, entityId);
            entityRecord.ArchetypeIndex = archetypeIndex;
            entityRecords[entityId] = entityRecord;
            return entity;
        }

        public void DestroyEntity(Entity entity)
        {
            if (!IsValid(entity))
            {
                throw new InvalidOperationException("Invalid entity.");
            }

            int entityId = entity.Id;
            var entityRecord = entityRecords[entityId];
            ref var archetype = ref archeTypes.Get(entityRecord.ArchetypeIndex);
            int swappedEntityId = archetype.RemoveEntity(entityRecord);

            if (swappedEntityId != -1)
            {
                ref var swappedRecord = ref entityRecords.Get(swappedEntityId);
                swappedRecord.ComponentIndex = entityRecord.ComponentIndex;
            }

            entities[entityId] = new Entity(entityId, entity.Generation + 1);
            recycledEntityIds.Push(entityId);
        }

        public T GetComponent<T>(Entity entity) where T : unmanaged, IComponent
        {
            return GetComponentRef<T>(entity);
        }

        public ref T GetComponentRef<T>(Entity entity) where T : unmanaged, IComponent
        {
            if (!IsValid(entity)) throw new InvalidOperationException("Invalid entity.");

            var entityRecord = entityRecords[entity.Id];
            return ref archeTypes[entityRecord.ArchetypeIndex].GetComponent<T>(entityRecord);
        }

        public Buffer<T> GetBuffer<T>(Entity entity) where T : unmanaged, IBufferComponent
        {
            if (!IsValid(entity)) throw new InvalidOperationException("Invalid entity.");
            var record = entityRecords[entity.Id];
            return archeTypes[record.ArchetypeIndex].GetBuffer<T>(record);
        }

        public bool IsValid(Entity entity)
        {
            if (entity.Id < 0 || entity.Id >= entities.Count) return false;
            return entities[entity.Id].Generation == entity.Generation;
        }

        public QueryBuilder Query()
        {
            return new QueryBuilder(ecsMemoryState.PopQueryBuilderHandle(), ecsMemoryState);
        }

        public ref Archetype GetArchetype(int index)
        {
            if (index < 0 || index >= archeTypes.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Invalid archetype index {index}.");
            }

            return ref archeTypes.Get(index);
        }

        public Entity GetEntity(int entityId)
        {
            if (entityId < 0 || entityId >= entities.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(entityId), $"Invalid entity id {entityId}.");
            }

            return entities[entityId];
        }

        private bool MatchesQuery(in QueryBuilder query, ref Archetype archetype)
        {
            for (int i = 0; i < query.allTypeIds.Count; i++)
            {
                if (!archetype.HasComponentTypeId(query.allTypeIds[i]))
                {
                    return false;
                }
            }

            if (query.anyTypeIds.Count > 0)
            {
                bool anyMatch = false;
                for (int i = 0; i < query.anyTypeIds.Count; i++)
                {
                    if (archetype.HasComponentTypeId(query.anyTypeIds[i]))
                    {
                        anyMatch = true;
                        break;
                    }
                }

                if (!anyMatch)
                {
                    return false;
                }
            }

            for (int i = 0; i < query.noneTypeIds.Count; i++)
            {
                if (archetype.HasComponentTypeId(query.noneTypeIds[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public QueryJobHandle Run<TDispatch>(in QueryBuilder query, ref TDispatch dispatch)
            where TDispatch : struct, IQueryJobDispatch
        {
            var arenaHandle = ecsMemoryState.PopSystemArenaHandle();

            // Compute upper bound of total chunks across all archetypes
            int maxChunks = 1;
            for (int a = 0; a < archeTypes.Count; a++)
                maxChunks += archeTypes.Get(a).ChunkCount;

            dispatch.Prepare(arenaHandle, ecsMemoryState, maxChunks);

            int chunkIndex = 0;
            for (int a = 0; a < archeTypes.Count; a++)
            {
                ref var archetype = ref archeTypes.Get(a);
                if (!MatchesQuery(query, ref archetype)) continue;
                if (!dispatch.PrepareArchetype(ref archetype)) continue;

                for (int c = 0; c < archetype.ChunkCount; c++)
                {
                    ref var chunk = ref archetype.GetChunk(c);
                    if (chunk.EntityCount == 0) continue;
                    dispatch.CollectChunk(ref chunk, chunkIndex);
                    chunkIndex++;
                }
            }

            var jobHandle = dispatch.ScheduleParallel(chunkIndex);
            return new QueryJobHandle(jobHandle, arenaHandle, ecsMemoryState);
        }

        public QueryJobHandle RunNonBurst<TDispatch>(in QueryBuilder query, ref TDispatch dispatch)
            where TDispatch : struct, IQueryJobDispatch
        {
            var arenaHandle = ecsMemoryState.PopSystemArenaHandle();

            int maxChunks = 1;
            for (int a = 0; a < archeTypes.Count; a++)
                maxChunks += archeTypes.Get(a).ChunkCount;

            dispatch.Prepare(arenaHandle, ecsMemoryState, maxChunks);

            int chunkIndex = 0;
            for (int a = 0; a < archeTypes.Count; a++)
            {
                ref var archetype = ref archeTypes.Get(a);
                if (!MatchesQuery(query, ref archetype)) continue;
                if (!dispatch.PrepareArchetype(ref archetype)) continue;

                for (int c = 0; c < archetype.ChunkCount; c++)
                {
                    ref var chunk = ref archetype.GetChunk(c);
                    if (chunk.EntityCount == 0) continue;
                    dispatch.CollectChunk(ref chunk, chunkIndex);
                    chunkIndex++;
                }
            }

            dispatch.RunNonBurst(chunkIndex);
            return new QueryJobHandle(default, arenaHandle, ecsMemoryState);
        }

    }
}
