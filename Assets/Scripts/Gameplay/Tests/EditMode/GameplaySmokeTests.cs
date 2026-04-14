using System;
using System.Linq;
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

        [Test]
        public void GameplayAssembly_IsPresentInCurrentAppDomain()
        {
            var assembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Glai.Gameplay");

            Assert.IsNotNull(assembly);
        }

        [Test]
        public void GameplayAssembly_GetTypes_DoesNotThrow()
        {
            var assembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Glai.Gameplay");

            Assert.IsNotNull(assembly);
            Assert.DoesNotThrow(() => assembly.GetTypes());
        }
    }
}
