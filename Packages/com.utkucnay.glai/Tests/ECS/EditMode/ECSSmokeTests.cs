using System;
using System.Runtime.CompilerServices;
using Glai.ECS;
using Glai.ECS.Core;
using NUnit.Framework;
using Unity.Burst;

namespace Glai.ECS.Tests.EditMode
{
    // Component types must be non-nested for the source generator to reference them
    // in the generated dispatch structs.

    internal struct Position : IComponent
    {
        public int Value;
    }

    internal struct Scale : IComponent
    {
        public int Value;
    }

    internal struct Velocity : IComponent
    {
        public int Value;
    }

    internal struct Comp1 : IComponent { public int Value; }
    internal struct Comp2 : IComponent { public int Value; }
    internal struct Comp3 : IComponent { public int Value; }
    internal struct Comp4 : IComponent { public int Value; }
    internal struct Comp5 : IComponent { public int Value; }
    internal struct Comp6 : IComponent { public int Value; }
    internal struct Comp7 : IComponent { public int Value; }

    // System structs must be non-nested so the source generator can emit
    // concrete [BurstCompile] dispatch wrappers for them.

    [QueryJob(QueryExecution.ChunkParallel), BurstCompile]
    internal struct AddTenSystem
    {
        public void Execute(Ref<Position> c1)
        {
            c1.Value.Value += 10;
        }
    }

    [QueryJob(QueryExecution.ChunkParallel), BurstCompile]
    internal struct DoublePositionScaleSystem
    {
        public void Execute(Ref<Position> c1, Ref<Scale> c2)
        {
            c1.Value.Value *= 2;
            c2.Value.Value *= 2;
        }
    }

    [QueryJob(QueryExecution.ChunkParallel), BurstCompile]
    internal struct SetOneSystem
    {
        public void Execute(Ref<Position> c1, Ref<Scale> c2)
        {
            c1.Value.Value = 1;
            c2.Value.Value = 1;
        }
    }

    [QueryJob(QueryExecution.ChunkParallel), BurstCompile]
    internal struct AddTenCountSystem
    {
        public void Execute(Ref<Position> c1)
        {
            c1.Value.Value += 10;
        }
    }

    [QueryJob(QueryExecution.MainThread), NonBurstQuery]
    internal struct WriteEntityIdSystem
    {
        public void Execute(int entityId, Ref<Position> c1)
        {
            c1.Value.Value = entityId;
        }
    }

    [QueryJob(QueryExecution.MainThread), NonBurstQuery]
    internal struct WriteUniqueEntityPhaseSystem
    {
        public void Execute(int entityId, Ref<Position> c1)
        {
            c1.Value.Value = entityId * 17 + 3;
        }
    }

    [QueryJob(QueryExecution.ChunkParallel), BurstCompile]
    internal struct IncrementAll7System
    {
        public void Execute(Ref<Comp1> c1, Ref<Comp2> c2, Ref<Comp3> c3, Ref<Comp4> c4, Ref<Comp5> c5, Ref<Comp6> c6, Ref<Comp7> c7)
        {
            c1.Value.Value += 1;
            c2.Value.Value += 1;
            c3.Value.Value += 1;
            c4.Value.Value += 1;
            c5.Value.Value += 1;
            c6.Value.Value += 1;
            c7.Value.Value += 1;
        }
    }

    public class ECSSmokeTests
    {
        [SetUp]
        public void SetUp()
        {
            DisableLogAndWarning();
        }

        [TearDown]
        public void TearDown()
        {
            ResetLoggerChannels();
        }

        private static void DisableLogAndWarning()
        {
            Glai.Core.Logger.EnableLog = false;
            Glai.Core.Logger.EnableWarning = false;
        }

        private static void ResetLoggerChannels()
        {
            Glai.Core.Logger.ResetChannels();
        }

        [Test]
        public void Entity_Equality_UsesIdAndGeneration()
        {
            var a = new Entity(3, 1);
            var b = new Entity(3, 1);
            var c = new Entity(3, 2);

            Assert.IsTrue(a.Equals(b));
            Assert.IsFalse(a.Equals(c));
        }

        [Test]
        public void EntityManager_CreateArchetypeAndEntity_ProducesValidEntity()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var entity = manager.CreateEntity(archetype);

            Assert.IsTrue(manager.IsValid(entity));
            manager.Dispose();
        }

        [Test]
        public void EntityManager_CreateEntity_WithInvalidArchetype_Throws()
        {
            var manager = new EntityManager();
            manager.Initialize();

            Assert.Throws<ArgumentOutOfRangeException>(() => manager.CreateEntity(-1));
            manager.Dispose();
        }

        [Test]
        public void EntityManager_CreateArchetype_WithDuplicateComponentType_Throws()
        {
            var manager = new EntityManager();
            manager.Initialize();

            Assert.Throws<InvalidOperationException>(() => manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>(), ArchetypeType.Component<Position>() }));
            manager.Dispose();
        }

        [Test]
        public void EntityManager_CreateArchetype_WithEmptyTypeSpan_Throws()
        {
            var manager = new EntityManager();
            manager.Initialize();

            Assert.Throws<InvalidOperationException>(() => manager.CreateArchetype(ReadOnlySpan<ArchetypeType>.Empty));
            manager.Dispose();
        }

        [Test]
        public void EntityManager_GetComponentRef_PersistsMutation()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var entity = manager.CreateEntity(archetype);

            ref var position = ref manager.GetComponentRef<Position>(entity);
            position.Value = 42;

            Assert.AreEqual(42, manager.GetComponent<Position>(entity).Value);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_MultipleEntities_DoNotShareComponentStorage()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var first = manager.CreateEntity(archetype);
            var second = manager.CreateEntity(archetype);

            ref var firstPosition = ref manager.GetComponentRef<Position>(first);
            ref var secondPosition = ref manager.GetComponentRef<Position>(second);

            firstPosition.Value = 11;
            secondPosition.Value = 22;

            Assert.AreEqual(11, manager.GetComponent<Position>(first).Value);
            Assert.AreEqual(22, manager.GetComponent<Position>(second).Value);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_CreateEntity_WhenStorageIsFull_ThrowsWithoutCorruptingExistingEntities()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var first = manager.CreateEntity(archetype);
            manager.GetComponentRef<Position>(first).Value = 7;

            for (int i = 1; i < 300_000; i++)
            {
                manager.CreateEntity(archetype);
            }

            Assert.Throws<InvalidOperationException>(() => manager.CreateEntity(archetype));
            Assert.AreEqual(7, manager.GetComponent<Position>(first).Value);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_NewEntity_ComponentIsZeroInitialized()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var entity = manager.CreateEntity(archetype);

            Assert.AreEqual(0, manager.GetComponent<Position>(entity).Value);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_ReusedEntitySlot_ComponentIsResetToZero()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var first = manager.CreateEntity(archetype);
            manager.GetComponentRef<Position>(first).Value = 42;

            manager.DestroyEntity(first);

            var recycled = manager.CreateEntity(archetype);

            Assert.AreEqual(0, manager.GetComponent<Position>(recycled).Value);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_ReusedSwapBackSlot_ComponentIsResetToZero()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var first = manager.CreateEntity(archetype);
            var second = manager.CreateEntity(archetype);

            manager.GetComponentRef<Position>(first).Value = 11;
            manager.GetComponentRef<Position>(second).Value = 22;

            manager.DestroyEntity(first);

            var recycled = manager.CreateEntity(archetype);

            Assert.AreEqual(0, manager.GetComponent<Position>(recycled).Value);
            Assert.AreEqual(22, manager.GetComponent<Position>(second).Value);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_GetComponent_WithInvalidEntity_ThrowsInvalidOperation()
        {
            var manager = new EntityManager();
            manager.Initialize();

            Assert.Throws<InvalidOperationException>(() => manager.GetComponent<Position>(new Entity(-1)));
            manager.Dispose();
        }

        [Test]
        public void EntityManager_DestroyEntity_InvalidatesOldHandle_AndReusesIdWithNewGeneration()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var entity = manager.CreateEntity(archetype);
            manager.GetComponentRef<Position>(entity).Value = 4;

            manager.DestroyEntity(entity);

            Assert.IsFalse(manager.IsValid(entity));
            Assert.Throws<InvalidOperationException>(() => manager.GetComponent<Position>(entity));

            var recycledEntity = manager.CreateEntity(archetype);
            Assert.AreEqual(entity.Id, recycledEntity.Id);
            Assert.AreNotEqual(entity.Generation, recycledEntity.Generation);

            manager.GetComponentRef<Position>(recycledEntity).Value = 9;
            Assert.AreEqual(9, manager.GetComponent<Position>(recycledEntity).Value);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_DestroyEntity_WithInvalidEntity_ThrowsInvalidOperation()
        {
            var manager = new EntityManager();
            manager.Initialize();

            Assert.Throws<InvalidOperationException>(() => manager.DestroyEntity(new Entity(-1)));
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Run_WithAllSingleComponent_SystemMutatesComponent()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var entity = manager.CreateEntity(archetype);
            manager.GetComponentRef<Position>(entity).Value = 5;

            var query = manager.Query().WithAll<Position>();
            var system = new AddTenSystem();
            var handle = manager.Run(query, ref system);
            handle.Complete();

            Assert.AreEqual(15, manager.GetComponent<Position>(entity).Value);

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_RunNonBurst_WithAllSingleComponent_SystemMutatesComponent()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var entity = manager.CreateEntity(archetype);
            manager.GetComponentRef<Position>(entity).Value = 5;

            var query = manager.Query().WithAll<Position>();
            var system = new AddTenSystem();
            var handle = manager.RunNonBurst(query, ref system);
            handle.Complete();

            Assert.AreEqual(15, manager.GetComponent<Position>(entity).Value);

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_RunNonBurst_WithEntityIdInjected_WritesEntityIdsToComponents()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var first = manager.CreateEntity(archetype);
            var second = manager.CreateEntity(archetype);

            manager.GetComponentRef<Position>(first).Value = -1;
            manager.GetComponentRef<Position>(second).Value = -1;

            var query = manager.Query().WithAll<Position>();
            var system = new WriteEntityIdSystem();
            var handle = manager.RunNonBurst(query, ref system);
            handle.Complete();

            Assert.AreEqual(first.Id, manager.GetComponent<Position>(first).Value);
            Assert.AreEqual(second.Id, manager.GetComponent<Position>(second).Value);

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_RunNonBurst_WithEntityIdInjected_CanGenerateUniqueValuesPerEntity()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var first = manager.CreateEntity(archetype);
            var second = manager.CreateEntity(archetype);

            var query = manager.Query().WithAll<Position>();
            var system = new WriteUniqueEntityPhaseSystem();
            var handle = manager.RunNonBurst(query, ref system);
            handle.Complete();

            int firstValue = manager.GetComponent<Position>(first).Value;
            int secondValue = manager.GetComponent<Position>(second).Value;

            Assert.AreEqual(first.Id * 17 + 3, firstValue);
            Assert.AreEqual(second.Id * 17 + 3, secondValue);
            Assert.AreNotEqual(firstValue, secondValue);

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Run_WithAllTwoComponents_OnlyAffectsMatchingArchetypes()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int positionOnlyArchetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            int positionScaleArchetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>(), ArchetypeType.Component<Scale>() });

            var positionOnlyEntity = manager.CreateEntity(positionOnlyArchetype);
            var matchingEntity = manager.CreateEntity(positionScaleArchetype);

            manager.GetComponentRef<Position>(positionOnlyEntity).Value = 1;
            manager.GetComponentRef<Position>(matchingEntity).Value = 2;
            manager.GetComponentRef<Scale>(matchingEntity).Value = 3;

            var query = manager.Query().WithAll<Position, Scale>();
            var system = new DoublePositionScaleSystem();
            var handle = manager.Run(query, ref system);
            handle.Complete();

            Assert.AreEqual(1, manager.GetComponent<Position>(positionOnlyEntity).Value);
            Assert.AreEqual(4, manager.GetComponent<Position>(matchingEntity).Value);
            Assert.AreEqual(6, manager.GetComponent<Scale>(matchingEntity).Value);

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_RunNonBurst_WithAllTwoComponents_OnlyAffectsMatchingArchetypes()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int positionOnlyArchetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            int positionScaleArchetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>(), ArchetypeType.Component<Scale>() });

            var positionOnlyEntity = manager.CreateEntity(positionOnlyArchetype);
            var matchingEntity = manager.CreateEntity(positionScaleArchetype);

            manager.GetComponentRef<Position>(positionOnlyEntity).Value = 1;
            manager.GetComponentRef<Position>(matchingEntity).Value = 2;
            manager.GetComponentRef<Scale>(matchingEntity).Value = 3;

            var query = manager.Query().WithAll<Position, Scale>();
            var system = new DoublePositionScaleSystem();
            var handle = manager.RunNonBurst(query, ref system);
            handle.Complete();

            Assert.AreEqual(1, manager.GetComponent<Position>(positionOnlyEntity).Value);
            Assert.AreEqual(4, manager.GetComponent<Position>(matchingEntity).Value);
            Assert.AreEqual(6, manager.GetComponent<Scale>(matchingEntity).Value);

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Run_WithAll_SkipsDestroyedEntities()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>(), ArchetypeType.Component<Scale>() });
            var toDestroy = manager.CreateEntity(archetype);
            var alive = manager.CreateEntity(archetype);

            manager.DestroyEntity(toDestroy);

            var query = manager.Query().WithAll<Position, Scale>();
            var system = new SetOneSystem();
            var handle = manager.Run(query, ref system);
            handle.Complete();

            Assert.AreEqual(1, manager.GetComponent<Position>(alive).Value);
            Assert.AreEqual(1, manager.GetComponent<Scale>(alive).Value);

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Run_WithAnyAndWithNone_FiltersCorrectly()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int positionScale = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>(), ArchetypeType.Component<Scale>() });
            int positionOnly = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            int scaleOnly = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Scale>() });

            var entityA = manager.CreateEntity(positionScale);
            var entityB = manager.CreateEntity(positionOnly);
            var entityC = manager.CreateEntity(scaleOnly);

            manager.GetComponentRef<Position>(entityA).Value = 1;
            manager.GetComponentRef<Position>(entityB).Value = 2;
            manager.GetComponentRef<Scale>(entityA).Value = 3;
            manager.GetComponentRef<Scale>(entityC).Value = 4;

            var query = manager.Query()
                .WithAny<Position, Scale>()
                .WithNone<Scale>();
            var system = new AddTenCountSystem();
            var handle = manager.Run(query, ref system);
            handle.Complete();

            Assert.AreEqual(1, manager.GetComponent<Position>(entityA).Value);
            Assert.AreEqual(12, manager.GetComponent<Position>(entityB).Value);
            Assert.AreEqual(4, manager.GetComponent<Scale>(entityC).Value);

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Run_T7_SystemMutatesAllComponents()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetypeT7 = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Comp1>(), ArchetypeType.Component<Comp2>(), ArchetypeType.Component<Comp3>(), ArchetypeType.Component<Comp4>(), ArchetypeType.Component<Comp5>(), ArchetypeType.Component<Comp6>(), ArchetypeType.Component<Comp7>() });
            int archetypeT6 = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Comp1>(), ArchetypeType.Component<Comp2>(), ArchetypeType.Component<Comp3>(), ArchetypeType.Component<Comp4>(), ArchetypeType.Component<Comp5>(), ArchetypeType.Component<Comp6>() });

            var matchingEntity = manager.CreateEntity(archetypeT7);
            var nonMatchingEntity = manager.CreateEntity(archetypeT6);

            manager.GetComponentRef<Comp1>(matchingEntity).Value = 1;
            manager.GetComponentRef<Comp2>(matchingEntity).Value = 1;
            manager.GetComponentRef<Comp3>(matchingEntity).Value = 1;
            manager.GetComponentRef<Comp4>(matchingEntity).Value = 1;
            manager.GetComponentRef<Comp5>(matchingEntity).Value = 1;
            manager.GetComponentRef<Comp6>(matchingEntity).Value = 1;
            manager.GetComponentRef<Comp7>(matchingEntity).Value = 1;

            manager.GetComponentRef<Comp1>(nonMatchingEntity).Value = 5;

            var query = manager.Query()
                .WithAny<Comp1, Comp2, Comp3, Comp4, Comp5, Comp6, Comp7>()
                .WithNone<Scale>();
            var system = new IncrementAll7System();
            var handle = manager.Run(query, ref system);
            handle.Complete();

            Assert.AreEqual(2, manager.GetComponent<Comp1>(matchingEntity).Value);
            Assert.AreEqual(2, manager.GetComponent<Comp7>(matchingEntity).Value);
            Assert.AreEqual(5, manager.GetComponent<Comp1>(nonMatchingEntity).Value);

            handle.Dispose();
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Dispose_ClearsSingletonInstance()
        {
            var manager = new EntityManager();
            manager.Initialize();

            Assert.AreSame(manager, IEntityManager.Instance);

            manager.Dispose();

            Assert.IsNull(IEntityManager.Instance);
        }

        [Test]
        public void EntityManager_Run_ReturnsHandle_CompleteAndDispose_Lifecycle()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var entity = manager.CreateEntity(archetype);
            manager.GetComponentRef<Position>(entity).Value = 0;

            var query = manager.Query().WithAll<Position>();
            var system = new AddTenSystem();

            // Schedule — job is in flight
            var handle = manager.Run(query, ref system);

            // Complete — wait for job to finish
            handle.Complete();

            // Results are now available
            Assert.AreEqual(10, manager.GetComponent<Position>(entity).Value);

            // Dispose — return arena + query to pools
            handle.Dispose();

            manager.Dispose();
        }

        [Test]
        public void EntityManager_RunNonBurst_QueryCacheRefreshesWhenNewMatchingArchetypeIsCreated()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int positionOnlyArchetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>() });
            var firstEntity = manager.CreateEntity(positionOnlyArchetype);
            manager.GetComponentRef<Position>(firstEntity).Value = 5;

            var query = manager.Query().WithAll<Position>();
            var system = new AddTenSystem();

            var firstHandle = manager.RunNonBurst(query, ref system);
            firstHandle.Complete();
            firstHandle.Dispose();

            int positionScaleArchetype = manager.CreateArchetype(stackalloc ArchetypeType[] { ArchetypeType.Component<Position>(), ArchetypeType.Component<Scale>() });
            var secondEntity = manager.CreateEntity(positionScaleArchetype);
            manager.GetComponentRef<Position>(secondEntity).Value = 1;

            query = manager.Query().WithAll<Position>();
            system = new AddTenSystem();
            var secondHandle = manager.RunNonBurst(query, ref system);
            secondHandle.Complete();

            Assert.AreEqual(25, manager.GetComponent<Position>(firstEntity).Value);
            Assert.AreEqual(11, manager.GetComponent<Position>(secondEntity).Value);

            secondHandle.Dispose();
            manager.Dispose();
        }
    }
}
