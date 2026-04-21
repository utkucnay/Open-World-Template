using System;
using System.Runtime.CompilerServices;
using Glai.ECS.Core;

namespace Glai.ECS
{
    public static partial class ECSAPI
    {
        static EntityManager Manager => (EntityManager)IEntityManager.Instance;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entity CreateEntity(int archetypeIndex) => Manager.CreateEntity(archetypeIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DestroyEntity(Entity entity) => Manager.DestroyEntity(entity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(Entity entity) => Manager.IsValid(entity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetComponent<T>(Entity entity) where T : unmanaged, IComponent
            => Manager.GetComponent<T>(entity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetComponentRef<T>(Entity entity) where T : unmanaged, IComponent
            => ref Manager.GetComponentRef<T>(entity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CreateArchetype(ReadOnlySpan<ArchetypeType> types)
            => Manager.CreateArchetype(types);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QueryBuilder Query() => Manager.Query();

        #region System Execution

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QueryJobHandle Run<TDispatch>(QueryBuilder query, ref TDispatch dispatch)
            where TDispatch : struct, IQueryJobDispatch
            => Manager.Run(query, ref dispatch);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QueryJobHandle RunNonBurst<TDispatch>(QueryBuilder query, ref TDispatch dispatch)
            where TDispatch : struct, IQueryJobDispatch
            => Manager.RunNonBurst(query, ref dispatch);

        #endregion
    }
}
