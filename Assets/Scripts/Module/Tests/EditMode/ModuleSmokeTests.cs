using System;
using Glai.Module;
using NUnit.Framework;
using UnityEngine;

namespace Glai.Module.Tests.EditMode
{
    public class ModuleSmokeTests
    {
        private sealed class UnregisteredTestModule : ModuleBase
        {
            public override void Initialize()
            {
            }
        }

        [SetUp]
        public void SetUp()
        {
            DisableLogAndWarning();

            if (ModuleManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(ModuleManager.Instance.gameObject);
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (ModuleManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(ModuleManager.Instance.gameObject);
            }

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
        public void ModuleRegisterAttribute_TargetsClasses()
        {
            var usage = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
                typeof(ModuleRegisterAttribute),
                typeof(AttributeUsageAttribute));

            Assert.IsNotNull(usage);
            Assert.AreEqual(AttributeTargets.Class, usage.ValidOn);
        }

        [Test]
        public void ModuleManager_Awake_InitializesModuleCollections()
        {
            var go = new GameObject("ModuleManager_Test");
            var manager = go.AddComponent<ModuleManager>();

            Assert.IsNotNull(manager.Modules);
            Assert.IsNotNull(manager.StartModules);
            Assert.IsNotNull(manager.TickModules);
        }

        [Test]
        public void ModuleManager_GetModule_ForUnregisteredType_Throws()
        {
            var go = new GameObject("ModuleManager_GetModule_Test");
            var manager = go.AddComponent<ModuleManager>();

            Assert.Throws<InvalidOperationException>(() => manager.GetModule<UnregisteredTestModule>());
        }
    }
}
