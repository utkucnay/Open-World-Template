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
            var loggerType = Type.GetType("Glai.Core.Logger, Glai.Core");
            if (loggerType == null) return;

            loggerType.GetProperty("EnableLog")?.SetValue(null, false);
            loggerType.GetProperty("EnableWarning")?.SetValue(null, false);
        }

        private static void ResetLoggerChannels()
        {
            var loggerType = Type.GetType("Glai.Core.Logger, Glai.Core");
            if (loggerType == null) return;

            loggerType.GetMethod("ResetChannels")?.Invoke(null, null);
        }

        private struct Position : IComponent
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
        public void EntityManager_Dispose_ClearsSingletonInstance()
        {
            var manager = new EntityManager();
            manager.Initialize();

            Assert.AreSame(manager, IEntityManager.Instance);

            manager.Dispose();

            Assert.IsNull(IEntityManager.Instance);
        }

        [Test]
        public void Query_MatchesArchetype_WithRequiredComponents()
        {
            var manager = new EntityManager();
            manager.Initialize();

            int positionOnly = manager.CreateArchetype(new Position());
            int positionVelocity = manager.CreateArchetype(new Position(), new Velocity());
            var query = new Query().Has<Position>().Has<Velocity>();

            Assert.IsFalse(manager.ArchetypeMatches(query, positionOnly));
            Assert.IsTrue(manager.ArchetypeMatches(query, positionVelocity));

            manager.Dispose();
        }

        private struct Velocity : IComponent
        {
            public int Value;
        }
    }
}
