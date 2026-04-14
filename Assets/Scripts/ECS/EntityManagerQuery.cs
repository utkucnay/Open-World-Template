using Glai.Core;
using Glai.ECS.Core;

namespace Glai.ECS
{
    public unsafe partial class EntityManager
    {
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

        public void Run<TJob, T1>(QueryBuilder query, ref TJob job)
            where TJob : struct, IQueryJob<T1>
            where T1 : unmanaged, IComponent
        {
            try
            {
                for (int a = 0; a < archeTypes.Count; a++)
                {
                    ref var archetype = ref archeTypes.Get(a);
                    if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1>()) continue;

                    int s1 = archetype.GetComponentStorageIndex(TypeId<T1>.Id);

                    for (int c = 0; c < archetype.ChunkCount; c++)
                    {
                        ref var chunk = ref archetype.GetChunk(c);
                        if (chunk.EntityCount == 0) continue;
                        byte* p1 = (byte*)chunk.DataPtr + s1 * chunk.ComponentRegionBytes;

                        QueryDispatchBurst.ExecuteChunk<TJob, T1>(ref job, p1, chunk.MaxComponentSize, chunk.EntityCount);
                    }
                }
            }
            finally
            {
                DisposeQuery(ref query);
            }
        }

        public void Run<TJob, T1, T2>(QueryBuilder query, ref TJob job)
            where TJob : struct, IQueryJob<T1, T2>
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            try
            {
                for (int a = 0; a < archeTypes.Count; a++)
                {
                    ref var archetype = ref archeTypes.Get(a);
                    if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1, T2>()) continue;

                    int s1 = archetype.GetComponentStorageIndex(TypeId<T1>.Id);
                    int s2 = archetype.GetComponentStorageIndex(TypeId<T2>.Id);

                    for (int c = 0; c < archetype.ChunkCount; c++)
                    {
                        ref var chunk = ref archetype.GetChunk(c);
                        if (chunk.EntityCount == 0) continue;
                        byte* p1 = (byte*)chunk.DataPtr + s1 * chunk.ComponentRegionBytes;
                        byte* p2 = (byte*)chunk.DataPtr + s2 * chunk.ComponentRegionBytes;

                        QueryDispatchBurst.ExecuteChunk<TJob, T1, T2>(ref job, p1, p2, chunk.MaxComponentSize, chunk.EntityCount);
                    }
                }
            }
            finally
            {
                DisposeQuery(ref query);
            }
        }

        public void Run<TJob, T1, T2, T3>(QueryBuilder query, ref TJob job)
            where TJob : struct, IQueryJob<T1, T2, T3>
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
        {
            try
            {
                for (int a = 0; a < archeTypes.Count; a++)
                {
                    ref var archetype = ref archeTypes.Get(a);
                    if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1, T2, T3>()) continue;

                    int s1 = archetype.GetComponentStorageIndex(TypeId<T1>.Id);
                    int s2 = archetype.GetComponentStorageIndex(TypeId<T2>.Id);
                    int s3 = archetype.GetComponentStorageIndex(TypeId<T3>.Id);

                    for (int c = 0; c < archetype.ChunkCount; c++)
                    {
                        ref var chunk = ref archetype.GetChunk(c);
                        if (chunk.EntityCount == 0) continue;
                        byte* p1 = (byte*)chunk.DataPtr + s1 * chunk.ComponentRegionBytes;
                        byte* p2 = (byte*)chunk.DataPtr + s2 * chunk.ComponentRegionBytes;
                        byte* p3 = (byte*)chunk.DataPtr + s3 * chunk.ComponentRegionBytes;

                        QueryDispatchBurst.ExecuteChunk<TJob, T1, T2, T3>(ref job, p1, p2, p3, chunk.MaxComponentSize, chunk.EntityCount);
                    }
                }
            }
            finally
            {
                DisposeQuery(ref query);
            }
        }

        public void Run<TJob, T1, T2, T3, T4>(QueryBuilder query, ref TJob job)
            where TJob : struct, IQueryJob<T1, T2, T3, T4>
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
        {
            try
            {
                for (int a = 0; a < archeTypes.Count; a++)
                {
                    ref var archetype = ref archeTypes.Get(a);
                    if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1, T2, T3, T4>()) continue;

                    int s1 = archetype.GetComponentStorageIndex(TypeId<T1>.Id);
                    int s2 = archetype.GetComponentStorageIndex(TypeId<T2>.Id);
                    int s3 = archetype.GetComponentStorageIndex(TypeId<T3>.Id);
                    int s4 = archetype.GetComponentStorageIndex(TypeId<T4>.Id);

                    for (int c = 0; c < archetype.ChunkCount; c++)
                    {
                        ref var chunk = ref archetype.GetChunk(c);
                        if (chunk.EntityCount == 0) continue;
                        byte* p1 = (byte*)chunk.DataPtr + s1 * chunk.ComponentRegionBytes;
                        byte* p2 = (byte*)chunk.DataPtr + s2 * chunk.ComponentRegionBytes;
                        byte* p3 = (byte*)chunk.DataPtr + s3 * chunk.ComponentRegionBytes;
                        byte* p4 = (byte*)chunk.DataPtr + s4 * chunk.ComponentRegionBytes;

                        QueryDispatchBurst.ExecuteChunk<TJob, T1, T2, T3, T4>(ref job, p1, p2, p3, p4, chunk.MaxComponentSize, chunk.EntityCount);
                    }
                }
            }
            finally
            {
                DisposeQuery(ref query);
            }
        }

        public void Run<TJob, T1, T2, T3, T4, T5>(QueryBuilder query, ref TJob job)
            where TJob : struct, IQueryJob<T1, T2, T3, T4, T5>
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
        {
            try
            {
                for (int a = 0; a < archeTypes.Count; a++)
                {
                    ref var archetype = ref archeTypes.Get(a);
                    if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1, T2, T3, T4, T5>()) continue;

                    int s1 = archetype.GetComponentStorageIndex(TypeId<T1>.Id);
                    int s2 = archetype.GetComponentStorageIndex(TypeId<T2>.Id);
                    int s3 = archetype.GetComponentStorageIndex(TypeId<T3>.Id);
                    int s4 = archetype.GetComponentStorageIndex(TypeId<T4>.Id);
                    int s5 = archetype.GetComponentStorageIndex(TypeId<T5>.Id);

                    for (int c = 0; c < archetype.ChunkCount; c++)
                    {
                        ref var chunk = ref archetype.GetChunk(c);
                        if (chunk.EntityCount == 0) continue;
                        byte* p1 = (byte*)chunk.DataPtr + s1 * chunk.ComponentRegionBytes;
                        byte* p2 = (byte*)chunk.DataPtr + s2 * chunk.ComponentRegionBytes;
                        byte* p3 = (byte*)chunk.DataPtr + s3 * chunk.ComponentRegionBytes;
                        byte* p4 = (byte*)chunk.DataPtr + s4 * chunk.ComponentRegionBytes;
                        byte* p5 = (byte*)chunk.DataPtr + s5 * chunk.ComponentRegionBytes;

                        QueryDispatchBurst.ExecuteChunk<TJob, T1, T2, T3, T4, T5>(ref job, p1, p2, p3, p4, p5, chunk.MaxComponentSize, chunk.EntityCount);
                    }
                }
            }
            finally
            {
                DisposeQuery(ref query);
            }
        }

        public void Run<TJob, T1, T2, T3, T4, T5, T6>(QueryBuilder query, ref TJob job)
            where TJob : struct, IQueryJob<T1, T2, T3, T4, T5, T6>
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
            where T6 : unmanaged, IComponent
        {
            try
            {
                for (int a = 0; a < archeTypes.Count; a++)
                {
                    ref var archetype = ref archeTypes.Get(a);
                    if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1, T2, T3, T4, T5, T6>()) continue;

                    int s1 = archetype.GetComponentStorageIndex(TypeId<T1>.Id);
                    int s2 = archetype.GetComponentStorageIndex(TypeId<T2>.Id);
                    int s3 = archetype.GetComponentStorageIndex(TypeId<T3>.Id);
                    int s4 = archetype.GetComponentStorageIndex(TypeId<T4>.Id);
                    int s5 = archetype.GetComponentStorageIndex(TypeId<T5>.Id);
                    int s6 = archetype.GetComponentStorageIndex(TypeId<T6>.Id);

                    for (int c = 0; c < archetype.ChunkCount; c++)
                    {
                        ref var chunk = ref archetype.GetChunk(c);
                        if (chunk.EntityCount == 0) continue;
                        byte* p1 = (byte*)chunk.DataPtr + s1 * chunk.ComponentRegionBytes;
                        byte* p2 = (byte*)chunk.DataPtr + s2 * chunk.ComponentRegionBytes;
                        byte* p3 = (byte*)chunk.DataPtr + s3 * chunk.ComponentRegionBytes;
                        byte* p4 = (byte*)chunk.DataPtr + s4 * chunk.ComponentRegionBytes;
                        byte* p5 = (byte*)chunk.DataPtr + s5 * chunk.ComponentRegionBytes;
                        byte* p6 = (byte*)chunk.DataPtr + s6 * chunk.ComponentRegionBytes;

                        QueryDispatchBurst.ExecuteChunk<TJob, T1, T2, T3, T4, T5, T6>(ref job, p1, p2, p3, p4, p5, p6, chunk.MaxComponentSize, chunk.EntityCount);
                    }
                }
            }
            finally
            {
                DisposeQuery(ref query);
            }
        }

        public void Run<TJob, T1, T2, T3, T4, T5, T6, T7>(QueryBuilder query, ref TJob job)
            where TJob : struct, IQueryJob<T1, T2, T3, T4, T5, T6, T7>
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
            where T6 : unmanaged, IComponent
            where T7 : unmanaged, IComponent
        {
            try
            {
                for (int a = 0; a < archeTypes.Count; a++)
                {
                    ref var archetype = ref archeTypes.Get(a);
                    if (!MatchesQuery(query, ref archetype) || !archetype.HasAll<T1, T2, T3, T4, T5, T6, T7>()) continue;

                    int s1 = archetype.GetComponentStorageIndex(TypeId<T1>.Id);
                    int s2 = archetype.GetComponentStorageIndex(TypeId<T2>.Id);
                    int s3 = archetype.GetComponentStorageIndex(TypeId<T3>.Id);
                    int s4 = archetype.GetComponentStorageIndex(TypeId<T4>.Id);
                    int s5 = archetype.GetComponentStorageIndex(TypeId<T5>.Id);
                    int s6 = archetype.GetComponentStorageIndex(TypeId<T6>.Id);
                    int s7 = archetype.GetComponentStorageIndex(TypeId<T7>.Id);

                    for (int c = 0; c < archetype.ChunkCount; c++)
                    {
                        ref var chunk = ref archetype.GetChunk(c);
                        if (chunk.EntityCount == 0) continue;
                        byte* p1 = (byte*)chunk.DataPtr + s1 * chunk.ComponentRegionBytes;
                        byte* p2 = (byte*)chunk.DataPtr + s2 * chunk.ComponentRegionBytes;
                        byte* p3 = (byte*)chunk.DataPtr + s3 * chunk.ComponentRegionBytes;
                        byte* p4 = (byte*)chunk.DataPtr + s4 * chunk.ComponentRegionBytes;
                        byte* p5 = (byte*)chunk.DataPtr + s5 * chunk.ComponentRegionBytes;
                        byte* p6 = (byte*)chunk.DataPtr + s6 * chunk.ComponentRegionBytes;
                        byte* p7 = (byte*)chunk.DataPtr + s7 * chunk.ComponentRegionBytes;

                        QueryDispatchBurst.ExecuteChunk<TJob, T1, T2, T3, T4, T5, T6, T7>(ref job, p1, p2, p3, p4, p5, p6, p7, chunk.MaxComponentSize, chunk.EntityCount);
                    }
                }
            }
            finally
            {
                DisposeQuery(ref query);
            }
        }
    }
}
