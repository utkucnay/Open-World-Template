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

        private sealed class RegisteredLifecycleModule : ModuleBase, IStart, ITick, ILateTick
        {
            public bool Initialized { get; private set; }

            public override void Initialize()
            {
                Initialized = true;
            }

            public void Start()
            {
            }

            public void Tick(float deltaTime)
            {
            }

            public void LateTick(float deltaTime)
            {
            }
        }

        [SetUp]
        public void SetUp()
        {
            DisableLogAndWarning();
            RuntimeModuleCatalog.ResetForTests();

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

            RuntimeModuleCatalog.ResetForTests();
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
        public void ModuleManager_Awake_InitializesModuleCollections()
        {
            var go = new GameObject("ModuleManager_Test");
            var manager = go.AddComponent<ModuleManager>();

            Assert.IsNotNull(manager.Modules);
            Assert.IsNotNull(manager.StartModules);
            Assert.IsNotNull(manager.TickModules);
        }

        [Test]
        public void ModuleCatalog_Register_AddsModuleDefinitionOnce()
        {
            RuntimeModuleCatalog.Register<RegisteredLifecycleModule>();
            RuntimeModuleCatalog.Register<RegisteredLifecycleModule>();

            Assert.That(RuntimeModuleCatalog.Definitions.Count, Is.EqualTo(1));
            Assert.That(RuntimeModuleCatalog.Definitions[0].ModuleType, Is.EqualTo(typeof(RegisteredLifecycleModule)));
        }

        [Test]
        public void ModuleManager_GetModule_ReturnsRegisteredModule()
        {
            RuntimeModuleCatalog.Register<RegisteredLifecycleModule>();

            var go = new GameObject("ModuleManager_GetRegistered_Test");
            var manager = go.AddComponent<ModuleManager>();

            Assert.That(manager.GetModule<RegisteredLifecycleModule>(), Is.Not.Null);
        }

        [Test]
        public void ModuleManager_LifecycleLists_IncludeRegisteredModule()
        {
            RuntimeModuleCatalog.Register<RegisteredLifecycleModule>();

            var go = new GameObject("ModuleManager_Lifecycle_Test");
            var manager = go.AddComponent<ModuleManager>();

            var module = manager.GetModule<RegisteredLifecycleModule>();

            Assert.That(module.Initialized, Is.True);
            Assert.That(manager.StartModules.Count, Is.EqualTo(1));
            Assert.That(manager.TickModules.Count, Is.EqualTo(1));
            Assert.That(manager.LateTickModules.Count, Is.EqualTo(1));
            Assert.That(manager.StartModules[0], Is.SameAs(module));
            Assert.That(manager.TickModules[0], Is.SameAs(module));
            Assert.That(manager.LateTickModules[0], Is.SameAs(module));
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
