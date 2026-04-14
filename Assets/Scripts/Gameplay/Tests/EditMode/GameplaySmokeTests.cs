using Glai.Gameplay;
using NUnit.Framework;

namespace Glai.Gameplay.Tests.EditMode
{
    public class GameplaySmokeTests
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
        public void GameplayAssembly_IsPresentInCurrentAppDomain()
        {
            var assembly = typeof(GameplayMarker).Assembly;

            Assert.IsNotNull(assembly);
            Assert.AreEqual("Glai.Gameplay", assembly.GetName().Name);
        }

        [Test]
        public void GameplayAssembly_GetTypes_DoesNotThrow()
        {
            var assembly = typeof(GameplayMarker).Assembly;

            Assert.IsNotNull(assembly);
            Assert.DoesNotThrow(() => assembly.GetTypes());
        }
    }
}
