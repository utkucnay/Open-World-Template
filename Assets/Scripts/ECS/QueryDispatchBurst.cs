using System.Runtime.CompilerServices;
using Unity.Burst;
using Glai.ECS.Core;

namespace Glai.ECS
{
    [BurstCompile]
    internal static unsafe class QueryDispatchBurst
    {
        [BurstCompile]
        public static void ExecuteChunk<TJob, T1>(ref TJob job, byte* c1, int stride, int count)
            where TJob : struct, IQueryJob<T1>
            where T1 : unmanaged, IComponent
        {
            for (int i = 0; i < count; i++)
            {
                job.Execute(ref Unsafe.AsRef<T1>(c1 + i * stride));
            }
        }

        [BurstCompile]
        public static void ExecuteChunk<TJob, T1, T2>(ref TJob job, byte* c1, byte* c2, int stride, int count)
            where TJob : struct, IQueryJob<T1, T2>
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
        {
            for (int i = 0; i < count; i++)
            {
                job.Execute(
                    ref Unsafe.AsRef<T1>(c1 + i * stride),
                    ref Unsafe.AsRef<T2>(c2 + i * stride));
            }
        }

        [BurstCompile]
        public static void ExecuteChunk<TJob, T1, T2, T3>(ref TJob job, byte* c1, byte* c2, byte* c3, int stride, int count)
            where TJob : struct, IQueryJob<T1, T2, T3>
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
        {
            for (int i = 0; i < count; i++)
            {
                job.Execute(
                    ref Unsafe.AsRef<T1>(c1 + i * stride),
                    ref Unsafe.AsRef<T2>(c2 + i * stride),
                    ref Unsafe.AsRef<T3>(c3 + i * stride));
            }
        }

        [BurstCompile]
        public static void ExecuteChunk<TJob, T1, T2, T3, T4>(ref TJob job, byte* c1, byte* c2, byte* c3, byte* c4, int stride, int count)
            where TJob : struct, IQueryJob<T1, T2, T3, T4>
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
        {
            for (int i = 0; i < count; i++)
            {
                job.Execute(
                    ref Unsafe.AsRef<T1>(c1 + i * stride),
                    ref Unsafe.AsRef<T2>(c2 + i * stride),
                    ref Unsafe.AsRef<T3>(c3 + i * stride),
                    ref Unsafe.AsRef<T4>(c4 + i * stride));
            }
        }

        [BurstCompile]
        public static void ExecuteChunk<TJob, T1, T2, T3, T4, T5>(ref TJob job, byte* c1, byte* c2, byte* c3, byte* c4, byte* c5, int stride, int count)
            where TJob : struct, IQueryJob<T1, T2, T3, T4, T5>
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
        {
            for (int i = 0; i < count; i++)
            {
                job.Execute(
                    ref Unsafe.AsRef<T1>(c1 + i * stride),
                    ref Unsafe.AsRef<T2>(c2 + i * stride),
                    ref Unsafe.AsRef<T3>(c3 + i * stride),
                    ref Unsafe.AsRef<T4>(c4 + i * stride),
                    ref Unsafe.AsRef<T5>(c5 + i * stride));
            }
        }

        [BurstCompile]
        public static void ExecuteChunk<TJob, T1, T2, T3, T4, T5, T6>(ref TJob job, byte* c1, byte* c2, byte* c3, byte* c4, byte* c5, byte* c6, int stride, int count)
            where TJob : struct, IQueryJob<T1, T2, T3, T4, T5, T6>
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
            where T6 : unmanaged, IComponent
        {
            for (int i = 0; i < count; i++)
            {
                job.Execute(
                    ref Unsafe.AsRef<T1>(c1 + i * stride),
                    ref Unsafe.AsRef<T2>(c2 + i * stride),
                    ref Unsafe.AsRef<T3>(c3 + i * stride),
                    ref Unsafe.AsRef<T4>(c4 + i * stride),
                    ref Unsafe.AsRef<T5>(c5 + i * stride),
                    ref Unsafe.AsRef<T6>(c6 + i * stride));
            }
        }

        [BurstCompile]
        public static void ExecuteChunk<TJob, T1, T2, T3, T4, T5, T6, T7>(ref TJob job, byte* c1, byte* c2, byte* c3, byte* c4, byte* c5, byte* c6, byte* c7, int stride, int count)
            where TJob : struct, IQueryJob<T1, T2, T3, T4, T5, T6, T7>
            where T1 : unmanaged, IComponent
            where T2 : unmanaged, IComponent
            where T3 : unmanaged, IComponent
            where T4 : unmanaged, IComponent
            where T5 : unmanaged, IComponent
            where T6 : unmanaged, IComponent
            where T7 : unmanaged, IComponent
        {
            for (int i = 0; i < count; i++)
            {
                job.Execute(
                    ref Unsafe.AsRef<T1>(c1 + i * stride),
                    ref Unsafe.AsRef<T2>(c2 + i * stride),
                    ref Unsafe.AsRef<T3>(c3 + i * stride),
                    ref Unsafe.AsRef<T4>(c4 + i * stride),
                    ref Unsafe.AsRef<T5>(c5 + i * stride),
                    ref Unsafe.AsRef<T6>(c6 + i * stride),
                    ref Unsafe.AsRef<T7>(c7 + i * stride));
            }
        }
    }
}
