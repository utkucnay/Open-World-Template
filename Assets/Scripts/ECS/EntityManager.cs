using System;
using Glai.Collection;
using Glai.ECS.Core;
using Glai.Module;

namespace Glai.ECS
{
    [ModuleRegister]
    public partial class EntityManager : ModuleBase, IEntityManager
    {
        FixedList<Archetype> archeTypes;
        FixedList<EntityRecord> entityRecords;
        FixedList<Entity> entities;
        FixedStack<int> recycledEntityIds;

        ECSMemoryState ecsMemoryState;
        public ECSMemoryState ECSMemoryState => ecsMemoryState;

        public override void Initialize()
        {
            IEntityManager.Instance = this;

            ecsMemoryState = new ECSMemoryState();
            archeTypes = new FixedList<Archetype>(100, ecsMemoryState.persistHandle, ecsMemoryState);
            entityRecords = new FixedList<EntityRecord>(100, ecsMemoryState.persistHandle, ecsMemoryState);
            entities = new FixedList<Entity>(100, ecsMemoryState.persistHandle, ecsMemoryState);
            recycledEntityIds = new FixedStack<int>(100, ecsMemoryState.persistHandle, ecsMemoryState);
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

#region Archetype Creation
        public int CreateArchetype<T1>(T1 t1) where T1 : unmanaged, IComponent
        {
            Span<int> componentTypeIds = stackalloc int[1];
            Span<int> componentSizes = stackalloc int[1];
            ArchetypeData archetypeData = new ArchetypeData(16, componentTypeIds, componentSizes);
            archetypeData.AddComponent<T1>();

            var archetype = new Archetype(archetypeData, ecsMemoryState.persistHandle, ecsMemoryState);
            archeTypes.Add(archetype);
            return archeTypes.Count - 1;
        }

        public int CreateArchetype<T1, T2>(T1 t1, T2 t2) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent
        {
            Span<int> componentTypeIds = stackalloc int[2];
            Span<int> componentSizes = stackalloc int[2];
            ArchetypeData archetypeData = new ArchetypeData(16, componentTypeIds, componentSizes);
            archetypeData.AddComponent<T1>();
            archetypeData.AddComponent<T2>();

            var archetype = new Archetype(archetypeData, ecsMemoryState.persistHandle, ecsMemoryState);
            archeTypes.Add(archetype);
            return archeTypes.Count - 1;
        }

        public int CreateArchetype<T1, T2, T3>(T1 t1, T2 t2, T3 t3) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent
        {
            Span<int> componentTypeIds = stackalloc int[3];
            Span<int> componentSizes = stackalloc int[3];
            ArchetypeData archetypeData = new ArchetypeData(16, componentTypeIds, componentSizes);
            archetypeData.AddComponent<T1>();
            archetypeData.AddComponent<T2>();
            archetypeData.AddComponent<T3>();

            var archetype = new Archetype(archetypeData, ecsMemoryState.persistHandle, ecsMemoryState);
            archeTypes.Add(archetype);
            return archeTypes.Count - 1;
        }

        public int CreateArchetype<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent
        {
            Span<int> componentTypeIds = stackalloc int[4];
            Span<int> componentSizes = stackalloc int[4];
            ArchetypeData archetypeData = new ArchetypeData(16, componentTypeIds, componentSizes);
            archetypeData.AddComponent<T1>();
            archetypeData.AddComponent<T2>();
            archetypeData.AddComponent<T3>();
            archetypeData.AddComponent<T4>();

            var archetype = new Archetype(archetypeData, ecsMemoryState.persistHandle, ecsMemoryState);
            archeTypes.Add(archetype);
            return archeTypes.Count - 1;
        }

        public int CreateArchetype<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent
        {
            Span<int> componentTypeIds = stackalloc int[5];
            Span<int> componentSizes = stackalloc int[5];
            ArchetypeData archetypeData = new ArchetypeData(16, componentTypeIds, componentSizes);
            archetypeData.AddComponent<T1>();
            archetypeData.AddComponent<T2>();
            archetypeData.AddComponent<T3>();
            archetypeData.AddComponent<T4>();
            archetypeData.AddComponent<T5>();

            var archetype = new Archetype(archetypeData, ecsMemoryState.persistHandle, ecsMemoryState);
            archeTypes.Add(archetype);
            return archeTypes.Count - 1;
        }

        public int CreateArchetype<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent
        {
            Span<int> componentTypeIds = stackalloc int[6];
            Span<int> componentSizes = stackalloc int[6];
            ArchetypeData archetypeData = new ArchetypeData(16, componentTypeIds, componentSizes);
            archetypeData.AddComponent<T1>();
            archetypeData.AddComponent<T2>();
            archetypeData.AddComponent<T3>();
            archetypeData.AddComponent<T4>();
            archetypeData.AddComponent<T5>();
            archetypeData.AddComponent<T6>();

            var archetype = new Archetype(archetypeData, ecsMemoryState.persistHandle, ecsMemoryState);
            archeTypes.Add(archetype);
            return archeTypes.Count - 1;
        }

        public int CreateArchetype<T1, T2, T3, T4, T5, T6, T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent where T7 : unmanaged, IComponent
        {
            Span<int> componentTypeIds = stackalloc int[7];
            Span<int> componentSizes = stackalloc int[7];
            ArchetypeData archetypeData = new ArchetypeData(16, componentTypeIds, componentSizes);
            archetypeData.AddComponent<T1>();
            archetypeData.AddComponent<T2>();
            archetypeData.AddComponent<T3>();
            archetypeData.AddComponent<T4>();
            archetypeData.AddComponent<T5>();
            archetypeData.AddComponent<T6>();
            archetypeData.AddComponent<T7>();

            var archetype = new Archetype(archetypeData, ecsMemoryState.persistHandle, ecsMemoryState);
            archeTypes.Add(archetype);
            return archeTypes.Count - 1;
        }
#endregion

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

        public bool IsValid(Entity entity)
        {
            if (entity.Id < 0 || entity.Id >= entities.Count) return false;
            if (recycledEntityIds.Contains(entity.Id)) return false;
            if (entities[entity.Id].Generation != entity.Generation) return false;
            return true;
        }

        public QueryBuilder Query()
        {
            return new QueryBuilder(ecsMemoryState.PopQueryBuilderHandle(), ecsMemoryState);
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

        public SystemHandle Run<TDispatch>(QueryBuilder query, ref TDispatch dispatch)
            where TDispatch : struct, ISystemDispatch
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
            return new SystemHandle(jobHandle, arenaHandle, ecsMemoryState, query);
        }
    }
}
