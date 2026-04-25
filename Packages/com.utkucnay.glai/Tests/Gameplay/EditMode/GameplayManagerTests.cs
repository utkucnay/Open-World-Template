using NUnit.Framework;

namespace Glai.Gameplay.Tests.EditMode
{
    public class GameplayManagerTests
    {
        private sealed class TestSystem : System
        {
            public int StartCount { get; private set; }
            public int TickCount { get; private set; }
            public int LateTickCount { get; private set; }

            public override void Start()
            {
                StartCount++;
            }

            public override void Tick(float deltaTime)
            {
                TickCount++;
            }

            public override void LateTick(float deltaTime)
            {
                LateTickCount++;
            }
        }

        GameplayManager manager;

        [SetUp]
        public void SetUp()
        {
            manager = new GameplayManager();
            manager.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            manager.Dispose();
        }

        [Test]
        public void Initialize_CreatesEmptySystemList()
        {
            Assert.IsNotNull(manager.systems);
            Assert.That(manager.systems, Is.Empty);
        }

        [Test]
        public void AddSystem_BeforeStart_StartsWhenManagerStarts()
        {
            var system = new TestSystem();

            manager.AddSystem(system);
            manager.Start();

            Assert.That(system.StartCount, Is.EqualTo(1));
        }

        [Test]
        public void AddSystem_AfterStart_StartsImmediately()
        {
            manager.Start();
            var system = new TestSystem();

            manager.AddSystem(system);

            Assert.That(system.StartCount, Is.EqualTo(1));
        }

        [Test]
        public void TickAndLateTick_ForwardsToSystems()
        {
            var system = new TestSystem();
            manager.AddSystem(system);

            manager.Tick(0.1f);
            manager.LateTick(0.1f);

            Assert.That(system.TickCount, Is.EqualTo(1));
            Assert.That(system.LateTickCount, Is.EqualTo(1));
        }
    }
}
