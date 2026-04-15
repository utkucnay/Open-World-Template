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

    // Job structs must be non-nested so the source generator can emit
    // concrete [BurstCompile] dispatch wrappers for them.

    [BurstCompile]
    internal struct AddTenJob : IQueryJob<Position>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(ref Position c1)
        {
            c1.Value += 10;
        }
    }

    [BurstCompile]
    internal struct DoublePositionScaleJob : IQueryJob<Position, Scale>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(ref Position c1, ref Scale c2)
        {
            c1.Value *= 2;
            c2.Value *= 2;
        }
    }

    [BurstCompile]
    internal struct SetOneJob : IQueryJob<Position, Scale>
    {
        public void Execute(ref Position c1, ref Scale c2)
        {
            c1.Value = 1;
            c2.Value = 1;
        }
    }

    [BurstCompile]
    internal struct AddTenCountJob : IQueryJob<Position>
    {
        public void Execute(ref Position c1)
        {
            c1.Value += 10;
        }
    }

    [BurstCompile]
    internal struct IncrementAll7Job : IQueryJob<Comp1, Comp2, Comp3, Comp4, Comp5, Comp6, Comp7>
    {
        int pos;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(ref Comp1 c1, ref Comp2 c2, ref Comp3 c3, ref Comp4 c4, ref Comp5 c5, ref Comp6 c6, ref Comp7 c7)
        {
            IncreasePosition();
            c1.Value += 1;
            c2.Value += 1;
            c3.Value += 1;
            c4.Value += 1;
            c5.Value += 1;
            c6.Value += 1;
            c7.Value += 1;
        }

        [BurstDiscard]
        public void IncreasePosition()
        {
            pos ++;
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

            int archetype = manager.CreateArchetype(new Position { Value = 5 });
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

            Assert.Throws<InvalidOperationException>(() => manager.CreateArchetype(new Position(), new Position()));
            manager.Dispose();
        }

        [Test]
        public void EntityManager_GetComponentRef_PersistsMutation()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(new Position());
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

            int archetype = manager.CreateArchetype(new Position());
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

            int archetype = manager.CreateArchetype(new Position());
            var first = manager.CreateEntity(archetype);
            manager.GetComponentRef<Position>(first).Value = 7;

            for (int i = 1; i < 100; i++)
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

            int archetype = manager.CreateArchetype(new Position());
            var entity = manager.CreateEntity(archetype);

            Assert.AreEqual(0, manager.GetComponent<Position>(entity).Value);
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

            int archetype = manager.CreateArchetype(new Position());
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
        public void EntityManager_Query_WithAllSingleComponent_JobMutatesComponent()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(new Position());
            var entity = manager.CreateEntity(archetype);
            manager.GetComponentRef<Position>(entity).Value = 5;

            var query = manager.Query().WithAll<Position>();
            var job = new AddTenJob();
            manager.Run(query, ref job);

            Assert.AreEqual(15, manager.GetComponent<Position>(entity).Value);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Query_WithAllTwoComponents_OnlyAffectsMatchingArchetypes()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int positionOnlyArchetype = manager.CreateArchetype(new Position());
            int positionScaleArchetype = manager.CreateArchetype(new Position(), new Scale());

            var positionOnlyEntity = manager.CreateEntity(positionOnlyArchetype);
            var matchingEntity = manager.CreateEntity(positionScaleArchetype);

            manager.GetComponentRef<Position>(positionOnlyEntity).Value = 1;
            manager.GetComponentRef<Position>(matchingEntity).Value = 2;
            manager.GetComponentRef<Scale>(matchingEntity).Value = 3;

            var query = manager.Query().WithAll<Position, Scale>();
            var job = new DoublePositionScaleJob();
            manager.Run(query, ref job);

            //Assert.AreEqual(1, job.Processed);
            Assert.AreEqual(1, manager.GetComponent<Position>(positionOnlyEntity).Value);
            Assert.AreEqual(4, manager.GetComponent<Position>(matchingEntity).Value);
            Assert.AreEqual(6, manager.GetComponent<Scale>(matchingEntity).Value);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Query_WithAll_SkipsDestroyedEntities()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(new Position(), new Scale());
            var toDestroy = manager.CreateEntity(archetype);
            var alive = manager.CreateEntity(archetype);

            manager.DestroyEntity(toDestroy);

            var query = manager.Query().WithAll<Position, Scale>();
            var job = new SetOneJob();
            manager.Run(query, ref job);

            //Assert.AreEqual(1, job.Processed);
            Assert.AreEqual(1, manager.GetComponent<Position>(alive).Value);
            Assert.AreEqual(1, manager.GetComponent<Scale>(alive).Value);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Query_WithAnyAndWithNone_FiltersCorrectly()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int positionScale = manager.CreateArchetype(new Position(), new Scale());
            int positionOnly = manager.CreateArchetype(new Position());
            int scaleOnly = manager.CreateArchetype(new Scale());

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
            var job = new AddTenCountJob();
            manager.Run(query, ref job);

            //Assert.AreEqual(1, job.Processed);
            Assert.AreEqual(1, manager.GetComponent<Position>(entityA).Value);
            Assert.AreEqual(12, manager.GetComponent<Position>(entityB).Value);
            Assert.AreEqual(4, manager.GetComponent<Scale>(entityC).Value);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Query_T7_JobMutatesAllComponents()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetypeT7 = manager.CreateArchetype(new Comp1(), new Comp2(), new Comp3(), new Comp4(), new Comp5(), new Comp6(), new Comp7());
            int archetypeT6 = manager.CreateArchetype(new Comp1(), new Comp2(), new Comp3(), new Comp4(), new Comp5(), new Comp6());

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
            var job = new IncrementAll7Job();
            manager.Run(query, ref job);

            Assert.AreEqual(2, manager.GetComponent<Comp1>(matchingEntity).Value);
            Assert.AreEqual(2, manager.GetComponent<Comp7>(matchingEntity).Value);
            Assert.AreEqual(5, manager.GetComponent<Comp1>(nonMatchingEntity).Value);
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
    }
}
