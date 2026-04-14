using System;
using Glai.Allocator;
using Glai.Collection;
using Glai.ECS.Core;

namespace Glai.ECS
{
    public struct QueryBuilder
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
    }
}
