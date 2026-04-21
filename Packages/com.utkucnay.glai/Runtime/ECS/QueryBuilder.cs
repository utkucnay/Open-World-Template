using System;
using Glai.Allocator;
using Glai.Collection;
using Glai.ECS.Core;

namespace Glai.ECS
{
    public ref partial struct QueryBuilder
    {
        public FixedList<int> allTypeIds;
        public FixedList<int> anyTypeIds;
        public FixedList<int> noneTypeIds;

        internal MemoryStateHandle memoryStateHandle;
        internal bool disposed;

        internal QueryBuilder(MemoryStateHandle memoryStateHandle, ECSMemoryState memoryState)
        {
            this.memoryStateHandle = memoryStateHandle;
            allTypeIds = new FixedList<int>(16, memoryStateHandle, memoryState);
            anyTypeIds = new FixedList<int>(16, memoryStateHandle, memoryState);
            noneTypeIds = new FixedList<int>(16, memoryStateHandle, memoryState);
            disposed = false;
        }

        public void Dispose()
        {
            var memoryState = IEntityManager.Instance.ECSMemoryState;
            if (!disposed)
            {
                noneTypeIds.Dispose(memoryState);
                anyTypeIds.Dispose(memoryState);
                allTypeIds.Dispose(memoryState);
                memoryState.PushQueryBuilderHandle(memoryStateHandle);
                disposed = true;
            }
        }

        private static void AddUniqueTypeId(ref FixedList<int> list, int typeId)
        {
            if (!list.Contains(typeId))
            {
                list.Add(typeId);
            }
        }

        private static void AddUnique<T>(ref FixedList<int> list)
            where T : unmanaged, IComponent
        {
            AddUniqueTypeId(ref list, Glai.Core.TypeId<T>.Id);
        }

        private static void AddUniqueBuffer<T>(ref FixedList<int> list)
            where T : unmanaged, IBufferComponent
        {
            AddUniqueTypeId(ref list, Glai.Core.TypeId<T>.Id);
        }

        /*
        * @template WithAll
        * hintName: Query.WithAll.Template.g.cs
        * namespace: Glai.ECS
        * container: public ref partial struct QueryBuilder
        * maxArity: 7
        * slot: T
        * constraint: unmanaged, IComponent
        * public QueryBuilder WithAll<{{GENERICS}}>()
        * {{WHERE_BLOCK}}
        * {
        *     {{#each T}}
        *     AddUnique<{{TYPE_NAME}}>(ref allTypeIds);
        *     {{/each}}
        *     return this;
        * }
        */

        /*
        * @template WithAny
        * hintName: Query.WithAny.Template.g.cs
        * namespace: Glai.ECS
        * container: public ref partial struct QueryBuilder
        * maxArity: 7
        * slot: T
        * constraint: unmanaged, IComponent
        * public QueryBuilder WithAny<{{GENERICS}}>()
        * {{WHERE_BLOCK}}
        * {
        *     {{#each T}}
        *     AddUnique<{{TYPE_NAME}}>(ref anyTypeIds);
        *     {{/each}}
        *     return this;
        * }
        */

        /*
        * @template WithNone
        * hintName: Query.WithNone.Template.g.cs
        * namespace: Glai.ECS
        * container: public ref partial struct QueryBuilder
        * maxArity: 7
        * slot: T
        * constraint: unmanaged, IComponent
        * public QueryBuilder WithNone<{{GENERICS}}>()
        * {{WHERE_BLOCK}}
        * {
        *     {{#each T}}
        *     AddUnique<{{TYPE_NAME}}>(ref noneTypeIds);
        *     {{/each}}
        *     return this;
        * }
        */

        public QueryBuilder WithAllBuffer<T1>() where T1 : unmanaged, IBufferComponent
        {
            AddUniqueBuffer<T1>(ref allTypeIds);
            return this;
        }

        public QueryBuilder WithAnyBuffer<T1>() where T1 : unmanaged, IBufferComponent
        {
            AddUniqueBuffer<T1>(ref anyTypeIds);
            return this;
        }

        public QueryBuilder WithNoneBuffer<T1>() where T1 : unmanaged, IBufferComponent
        {
            AddUniqueBuffer<T1>(ref noneTypeIds);
            return this;
        }
    }
}
