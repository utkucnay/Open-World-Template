using System;
using Glai.Collection;
using Glai.ECS.Core;
using Glai.Module;

namespace Glai.ECS
{
    [ModuleRegister]
    public class EntityManager : ModuleBase, IEntityManager
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

#region Archetype Creation
        public int CreateArchetype<T1>(T1 t1) where T1 : unmanaged, IComponent
        {
            Span<int> componentTypeIds = stackalloc int[1];
            ArchetypeData archetypeData = new ArchetypeData(16, componentTypeIds);
            archetypeData.AddComponent<T1>();

            var archetype = new Archetype(archetypeData, ecsMemoryState.persistHandle, ecsMemoryState);
            archeTypes.Add(archetype);
            return archeTypes.Count - 1;
        }

        public int CreateArchetype<T1, T2>(T1 t1, T2 t2) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent
        {
            Span<int> componentTypeIds = stackalloc int[2];
            ArchetypeData archetypeData = new ArchetypeData(16, componentTypeIds);
            archetypeData.AddComponent<T1>();
            archetypeData.AddComponent<T2>();

            var archetype = new Archetype(archetypeData, ecsMemoryState.persistHandle, ecsMemoryState);
            archeTypes.Add(archetype);
            return archeTypes.Count - 1;
        }

        public int CreateArchetype<T1, T2, T3>(T1 t1, T2 t2, T3 t3) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent
        {
            Span<int> componentTypeIds = stackalloc int[3];
            ArchetypeData archetypeData = new ArchetypeData(16, componentTypeIds);
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
            ArchetypeData archetypeData = new ArchetypeData(16, componentTypeIds);
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
            ArchetypeData archetypeData = new ArchetypeData(16, componentTypeIds);
            archetypeData.AddComponent<T1>();
            archetypeData.AddComponent<T2>();
            archetypeData.AddComponent<T3>();
            archetypeData.AddComponent<T4>();
            archetypeData.AddComponent<T5>();

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

            ref var archetype = ref archeTypes.Get(archetypeIndex);
            var entityRecord = archetype.AddEntity(ecsMemoryState);
            entityRecord.ArchetypeIndex = archetypeIndex;

            if (canRecycleEntityId)
            {
                int recycledEntityId = recycledEntityIds.Pop();

                try
                {
                    entityRecords[recycledEntityId] = entityRecord;
                    return entities[recycledEntityId];
                }
                catch
                {
                    recycledEntityIds.Push(recycledEntityId);
                    archetype.RemoveEntity(entityRecord);
                    throw;
                }
            }

            int entityRecordIndex = entityRecords.Count;

            try
            {
                entityRecords.Add(entityRecord);
            }
            catch
            {
                archetype.RemoveEntity(entityRecord);
                throw;
            }

            try
            {
                var entity = new Entity(entityRecordIndex);
                entities.Add(entity);
                return entity;
            }
            catch
            {
                entityRecords.RemoveAt(entityRecordIndex);
                archetype.RemoveEntity(entityRecord);
                throw;
            }
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
            if (entities[entity.Id].Generation != entity.Generation) return false;
            return true;
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
            archetype.RemoveEntity(entityRecord);

            entities[entityId] = new Entity(entityId, entity.Generation + 1);
            recycledEntityIds.Push(entityId);
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

        public bool ArchetypeMatches(Query query, int archetypeIndex)
        {
            if (archetypeIndex < 0 || archetypeIndex >= archeTypes.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(archetypeIndex), $"Invalid archetype index {archetypeIndex}.");
            }

            return query.Matches(archeTypes[archetypeIndex]);
        }
    }
}
