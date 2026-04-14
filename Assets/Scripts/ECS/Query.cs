using System;
using Glai.Allocator;
using Glai.Collection;
using Glai.ECS.Core;

namespace Glai.ECS
{
    public delegate void RefAction<T1>(ref T1 c1)
        where T1 : unmanaged, IComponent;

    public delegate void RefAction<T1, T2>(ref T1 c1, ref T2 c2)
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent;

    public delegate void RefAction<T1, T2, T3>(ref T1 c1, ref T2 c2, ref T3 c3)
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent;

    public delegate void RefAction<T1, T2, T3, T4>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent;

    public delegate void RefAction<T1, T2, T3, T4, T5>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5)
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent;

    public delegate void RefAction<T1, T2, T3, T4, T5, T6>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6)
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent;

    public delegate void RefAction<T1, T2, T3, T4, T5, T6, T7>(ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6, ref T7 c7)
        where T1 : unmanaged, IComponent
        where T2 : unmanaged, IComponent
        where T3 : unmanaged, IComponent
        where T4 : unmanaged, IComponent
        where T5 : unmanaged, IComponent
        where T6 : unmanaged, IComponent
        where T7 : unmanaged, IComponent;

    public struct QueryBuilder : IDisposable
    {
        public FixedList<int> allTypeIds;
        public FixedList<int> anyTypeIds;
        public FixedList<int> noneTypeIds;

        EntityManager manager;
        ECSMemoryState memoryState;
        MemoryStateHandle memoryStateHandle;
        bool disposed;

        internal QueryBuilder(EntityManager manager, MemoryStateHandle memoryStateHandle)
        {
            this.manager = manager;
            memoryState = manager.ECSMemoryState;
            this.memoryStateHandle = memoryStateHandle;
            allTypeIds = new FixedList<int>(16, memoryStateHandle, memoryState);
            anyTypeIds = new FixedList<int>(16, memoryStateHandle, memoryState);
            noneTypeIds = new FixedList<int>(16, memoryStateHandle, memoryState);
            disposed = false;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            noneTypeIds.Dispose(memoryState);
            anyTypeIds.Dispose(memoryState);
            allTypeIds.Dispose(memoryState);
            memoryState.PushQueryBuilderHandle(memoryStateHandle);
            disposed = true;
        }

        private static void AddUnique<T>(ref FixedList<int> list)
            where T : unmanaged, IComponent
        {
            int typeId = Glai.Core.TypeId<T>.Id;
            if (!list.Contains(typeId))
            {
                list.Add(typeId);
            }
        }

        public QueryBuilder WithAll<T1>() where T1 : unmanaged, IComponent
        {
            AddUnique<T1>(ref allTypeIds);
            return this;
        }

        public QueryBuilder WithAll<T1, T2>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent
        {
            AddUnique<T1>(ref allTypeIds);
            AddUnique<T2>(ref allTypeIds);
            return this;
        }

        public QueryBuilder WithAll<T1, T2, T3>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent
        {
            AddUnique<T1>(ref allTypeIds);
            AddUnique<T2>(ref allTypeIds);
            AddUnique<T3>(ref allTypeIds);
            return this;
        }

        public QueryBuilder WithAll<T1, T2, T3, T4>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent
        {
            AddUnique<T1>(ref allTypeIds);
            AddUnique<T2>(ref allTypeIds);
            AddUnique<T3>(ref allTypeIds);
            AddUnique<T4>(ref allTypeIds);
            return this;
        }

        public QueryBuilder WithAll<T1, T2, T3, T4, T5>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent
        {
            AddUnique<T1>(ref allTypeIds);
            AddUnique<T2>(ref allTypeIds);
            AddUnique<T3>(ref allTypeIds);
            AddUnique<T4>(ref allTypeIds);
            AddUnique<T5>(ref allTypeIds);
            return this;
        }

        public QueryBuilder WithAll<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent
        {
            AddUnique<T1>(ref allTypeIds);
            AddUnique<T2>(ref allTypeIds);
            AddUnique<T3>(ref allTypeIds);
            AddUnique<T4>(ref allTypeIds);
            AddUnique<T5>(ref allTypeIds);
            AddUnique<T6>(ref allTypeIds);
            return this;
        }

        public QueryBuilder WithAll<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent where T7 : unmanaged, IComponent
        {
            AddUnique<T1>(ref allTypeIds);
            AddUnique<T2>(ref allTypeIds);
            AddUnique<T3>(ref allTypeIds);
            AddUnique<T4>(ref allTypeIds);
            AddUnique<T5>(ref allTypeIds);
            AddUnique<T6>(ref allTypeIds);
            AddUnique<T7>(ref allTypeIds);
            return this;
        }

        public QueryBuilder WithAny<T1>() where T1 : unmanaged, IComponent
        {
            AddUnique<T1>(ref anyTypeIds);
            return this;
        }

        public QueryBuilder WithAny<T1, T2>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent
        {
            AddUnique<T1>(ref anyTypeIds);
            AddUnique<T2>(ref anyTypeIds);
            return this;
        }

        public QueryBuilder WithAny<T1, T2, T3>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent
        {
            AddUnique<T1>(ref anyTypeIds);
            AddUnique<T2>(ref anyTypeIds);
            AddUnique<T3>(ref anyTypeIds);
            return this;
        }

        public QueryBuilder WithAny<T1, T2, T3, T4>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent
        {
            AddUnique<T1>(ref anyTypeIds);
            AddUnique<T2>(ref anyTypeIds);
            AddUnique<T3>(ref anyTypeIds);
            AddUnique<T4>(ref anyTypeIds);
            return this;
        }

        public QueryBuilder WithAny<T1, T2, T3, T4, T5>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent
        {
            AddUnique<T1>(ref anyTypeIds);
            AddUnique<T2>(ref anyTypeIds);
            AddUnique<T3>(ref anyTypeIds);
            AddUnique<T4>(ref anyTypeIds);
            AddUnique<T5>(ref anyTypeIds);
            return this;
        }

        public QueryBuilder WithAny<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent
        {
            AddUnique<T1>(ref anyTypeIds);
            AddUnique<T2>(ref anyTypeIds);
            AddUnique<T3>(ref anyTypeIds);
            AddUnique<T4>(ref anyTypeIds);
            AddUnique<T5>(ref anyTypeIds);
            AddUnique<T6>(ref anyTypeIds);
            return this;
        }

        public QueryBuilder WithAny<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent where T7 : unmanaged, IComponent
        {
            AddUnique<T1>(ref anyTypeIds);
            AddUnique<T2>(ref anyTypeIds);
            AddUnique<T3>(ref anyTypeIds);
            AddUnique<T4>(ref anyTypeIds);
            AddUnique<T5>(ref anyTypeIds);
            AddUnique<T6>(ref anyTypeIds);
            AddUnique<T7>(ref anyTypeIds);
            return this;
        }

        public QueryBuilder WithNone<T1>() where T1 : unmanaged, IComponent
        {
            AddUnique<T1>(ref noneTypeIds);
            return this;
        }

        public QueryBuilder WithNone<T1, T2>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent
        {
            AddUnique<T1>(ref noneTypeIds);
            AddUnique<T2>(ref noneTypeIds);
            return this;
        }

        public QueryBuilder WithNone<T1, T2, T3>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent
        {
            AddUnique<T1>(ref noneTypeIds);
            AddUnique<T2>(ref noneTypeIds);
            AddUnique<T3>(ref noneTypeIds);
            return this;
        }

        public QueryBuilder WithNone<T1, T2, T3, T4>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent
        {
            AddUnique<T1>(ref noneTypeIds);
            AddUnique<T2>(ref noneTypeIds);
            AddUnique<T3>(ref noneTypeIds);
            AddUnique<T4>(ref noneTypeIds);
            return this;
        }

        public QueryBuilder WithNone<T1, T2, T3, T4, T5>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent
        {
            AddUnique<T1>(ref noneTypeIds);
            AddUnique<T2>(ref noneTypeIds);
            AddUnique<T3>(ref noneTypeIds);
            AddUnique<T4>(ref noneTypeIds);
            AddUnique<T5>(ref noneTypeIds);
            return this;
        }

        public QueryBuilder WithNone<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent
        {
            AddUnique<T1>(ref noneTypeIds);
            AddUnique<T2>(ref noneTypeIds);
            AddUnique<T3>(ref noneTypeIds);
            AddUnique<T4>(ref noneTypeIds);
            AddUnique<T5>(ref noneTypeIds);
            AddUnique<T6>(ref noneTypeIds);
            return this;
        }

        public QueryBuilder WithNone<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent where T7 : unmanaged, IComponent
        {
            AddUnique<T1>(ref noneTypeIds);
            AddUnique<T2>(ref noneTypeIds);
            AddUnique<T3>(ref noneTypeIds);
            AddUnique<T4>(ref noneTypeIds);
            AddUnique<T5>(ref noneTypeIds);
            AddUnique<T6>(ref noneTypeIds);
            AddUnique<T7>(ref noneTypeIds);
            return this;
        }

        public void ForEach<T1>(RefAction<T1> action) where T1 : unmanaged, IComponent
        {
            try
            {
                manager.ForEachQuery(this, action);
            }
            finally
            {
                Dispose();
            }
        }

        public void ForEach<T1, T2>(RefAction<T1, T2> action) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent
        {
            try
            {
                manager.ForEachQuery(this, action);
            }
            finally
            {
                Dispose();
            }
        }

        public void ForEach<T1, T2, T3>(RefAction<T1, T2, T3> action) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent
        {
            try
            {
                manager.ForEachQuery(this, action);
            }
            finally
            {
                Dispose();
            }
        }

        public void ForEach<T1, T2, T3, T4>(RefAction<T1, T2, T3, T4> action) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent
        {
            try
            {
                manager.ForEachQuery(this, action);
            }
            finally
            {
                Dispose();
            }
        }

        public void ForEach<T1, T2, T3, T4, T5>(RefAction<T1, T2, T3, T4, T5> action) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent
        {
            try
            {
                manager.ForEachQuery(this, action);
            }
            finally
            {
                Dispose();
            }
        }

        public void ForEach<T1, T2, T3, T4, T5, T6>(RefAction<T1, T2, T3, T4, T5, T6> action) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent
        {
            try
            {
                manager.ForEachQuery(this, action);
            }
            finally
            {
                Dispose();
            }
        }

        public void ForEach<T1, T2, T3, T4, T5, T6, T7>(RefAction<T1, T2, T3, T4, T5, T6, T7> action) where T1 : unmanaged, IComponent where T2 : unmanaged, IComponent where T3 : unmanaged, IComponent where T4 : unmanaged, IComponent where T5 : unmanaged, IComponent where T6 : unmanaged, IComponent where T7 : unmanaged, IComponent
        {
            try
            {
                manager.ForEachQuery(this, action);
            }
            finally
            {
                Dispose();
            }
        }
    }

    public partial class EntityManager
    {
        public QueryBuilder Query()
        {
            return new QueryBuilder(this, ecsMemoryState.PopQueryBuilderHandle());
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

        internal void ForEachQuery<T1>(QueryBuilder query, RefAction<T1> action)
            where T1 : unmanaged, IComponent
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!IsValid(entity)) continue;

                var entityRecord = entityRecords[i];
                ref var archetype = ref archeTypes.Get(entityRecord.ArchetypeIndex);
                if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1>()) continue;

                action(ref archetype.GetComponent<T1>(entityRecord));
            }
        }

        internal void ForEachQuery<T1, T2>(QueryBuilder query, RefAction<T1, T2> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!IsValid(entity)) continue;

                var entityRecord = entityRecords[i];
                ref var archetype = ref archeTypes.Get(entityRecord.ArchetypeIndex);
                if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1, T2>()) continue;

                action(ref archetype.GetComponent<T1>(entityRecord), ref archetype.GetComponent<T2>(entityRecord));
            }
        }

        internal void ForEachQuery<T1, T2, T3>(QueryBuilder query, RefAction<T1, T2, T3> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!IsValid(entity)) continue;

                var entityRecord = entityRecords[i];
                ref var archetype = ref archeTypes.Get(entityRecord.ArchetypeIndex);
                if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1, T2, T3>()) continue;

                action(ref archetype.GetComponent<T1>(entityRecord), ref archetype.GetComponent<T2>(entityRecord), ref archetype.GetComponent<T3>(entityRecord));
            }
        }

        internal void ForEachQuery<T1, T2, T3, T4>(QueryBuilder query, RefAction<T1, T2, T3, T4> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!IsValid(entity)) continue;

                var entityRecord = entityRecords[i];
                ref var archetype = ref archeTypes.Get(entityRecord.ArchetypeIndex);
                if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1, T2, T3, T4>()) continue;

                action(ref archetype.GetComponent<T1>(entityRecord), ref archetype.GetComponent<T2>(entityRecord), ref archetype.GetComponent<T3>(entityRecord), ref archetype.GetComponent<T4>(entityRecord));
            }
        }

        internal void ForEachQuery<T1, T2, T3, T4, T5>(QueryBuilder query, RefAction<T1, T2, T3, T4, T5> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!IsValid(entity)) continue;

                var entityRecord = entityRecords[i];
                ref var archetype = ref archeTypes.Get(entityRecord.ArchetypeIndex);
                if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1, T2, T3, T4, T5>()) continue;

                action(ref archetype.GetComponent<T1>(entityRecord), ref archetype.GetComponent<T2>(entityRecord), ref archetype.GetComponent<T3>(entityRecord), ref archetype.GetComponent<T4>(entityRecord), ref archetype.GetComponent<T5>(entityRecord));
            }
        }

        internal void ForEachQuery<T1, T2, T3, T4, T5, T6>(QueryBuilder query, RefAction<T1, T2, T3, T4, T5, T6> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
            where T6 : unmanaged, IComponent
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!IsValid(entity)) continue;

                var entityRecord = entityRecords[i];
                ref var archetype = ref archeTypes.Get(entityRecord.ArchetypeIndex);
                if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1, T2, T3, T4, T5, T6>()) continue;

                action(ref archetype.GetComponent<T1>(entityRecord), ref archetype.GetComponent<T2>(entityRecord), ref archetype.GetComponent<T3>(entityRecord), ref archetype.GetComponent<T4>(entityRecord), ref archetype.GetComponent<T5>(entityRecord), ref archetype.GetComponent<T6>(entityRecord));
            }
        }

        internal void ForEachQuery<T1, T2, T3, T4, T5, T6, T7>(QueryBuilder query, RefAction<T1, T2, T3, T4, T5, T6, T7> action)
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
            where T6 : unmanaged, IComponent
            where T7 : unmanaged, IComponent
        {
            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!IsValid(entity)) continue;

                var entityRecord = entityRecords[i];
                ref var archetype = ref archeTypes.Get(entityRecord.ArchetypeIndex);
                if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1, T2, T3, T4, T5, T6, T7>()) continue;

                action(ref archetype.GetComponent<T1>(entityRecord), ref archetype.GetComponent<T2>(entityRecord), ref archetype.GetComponent<T3>(entityRecord), ref archetype.GetComponent<T4>(entityRecord), ref archetype.GetComponent<T5>(entityRecord), ref archetype.GetComponent<T6>(entityRecord), ref archetype.GetComponent<T7>(entityRecord));
            }
        }
    }
}
