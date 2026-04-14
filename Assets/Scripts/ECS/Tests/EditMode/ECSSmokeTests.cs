using System;
using Glai.ECS;
using Glai.ECS.Core;
using NUnit.Framework;

namespace Glai.ECS.Tests.EditMode
{
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

        private struct Position : IComponent
        {
            public int Value;
        }

        private struct Scale : IComponent
        {
            public int Value;
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
        public void EntityManager_Query_WithAllSingleComponent_StaticLambdaMutatesComponent()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int archetype = manager.CreateArchetype(new Position());
            var entity = manager.CreateEntity(archetype);
            manager.GetComponentRef<Position>(entity).Value = 5;

            manager.Query()
                .WithAll<Position>()
                .ForEach(static (ref Position position) =>
                {
                    position.Value += 10;
                });

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

            int processed = 0;

            manager.Query()
                .WithAll<Position, Scale>()
                .ForEach((ref Position position, ref Scale scale) =>
                {
                    processed++;
                    position.Value *= 2;
                    scale.Value *= 2;
                });

            Assert.AreEqual(1, processed);
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

            int processed = 0;
            manager.Query()
                .WithAll<Position, Scale>()
                .ForEach((ref Position position, ref Scale scale) =>
                {
                    processed++;
                    position.Value = 1;
                    scale.Value = 1;
                });

            Assert.AreEqual(1, processed);
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

            int processed = 0;

            manager.Query()
                .WithAny<Position, Scale>()
                .WithNone<Scale>()
                .ForEach((ref Position position) =>
                {
                    processed++;
                    position.Value += 10;
                });

            Assert.AreEqual(1, processed);
            Assert.AreEqual(1, manager.GetComponent<Position>(entityA).Value);
            Assert.AreEqual(12, manager.GetComponent<Position>(entityB).Value);
            Assert.AreEqual(4, manager.GetComponent<Scale>(entityC).Value);
            manager.Dispose();
        }

        [Test]
        public void EntityManager_Query_T7_ForEachMutatesAllComponents()
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

            manager.Query()
                .WithAny<Comp1, Comp2, Comp3, Comp4, Comp5, Comp6, Comp7>()
                .WithNone<Scale>()
                .ForEach((ref Comp1 a, ref Comp2 b, ref Comp3 c, ref Comp4 d, ref Comp5 e, ref Comp6 f, ref Comp7 g) =>
                {
                    a.Value += 1;
                    b.Value += 1;
                    c.Value += 1;
                    d.Value += 1;
                    e.Value += 1;
                    f.Value += 1;
                    g.Value += 1;
                });

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

        private struct Velocity : IComponent
        {
            public int Value;
        }

        private struct Comp1 : IComponent { public int Value; }
        private struct Comp2 : IComponent { public int Value; }
        private struct Comp3 : IComponent { public int Value; }
        private struct Comp4 : IComponent { public int Value; }
        private struct Comp5 : IComponent { public int Value; }
        private struct Comp6 : IComponent { public int Value; }
        private struct Comp7 : IComponent { public int Value; }
    }
}
