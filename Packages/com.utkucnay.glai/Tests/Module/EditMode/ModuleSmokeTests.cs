using System;
using System.Linq;
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

        [ModuleRegister(priority: -10)]
        public sealed class RegisteredLifecycleModule : ModuleBase, IStart, ITick, ILateTick
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

        [ModuleRegister(priority: -20)]
        public sealed class EarlyRegisteredModule : ModuleBase
        {
            public override void Initialize()
            {
            }
        }

        [ModuleRegister(priority: -20)]
        public sealed class SamePriorityRegisteredModule : ModuleBase
        {
            public override void Initialize()
            {
            }
        }

        [SetUp]
        public void SetUp()
        {
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

        private static ModuleManager CreateManager(string name)
        {
            if (ModuleManager.Instance != null)
            {
                UnityEngine.Object.DestroyImmediate(ModuleManager.Instance.gameObject);
            }

            DisableLogAndWarning();
            var go = new GameObject(name);
            return go.AddComponent<ModuleManager>();
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
            var manager = CreateManager("ModuleManager_Test");

            Assert.IsNotNull(manager.Modules);
            Assert.IsNotNull(manager.StartModules);
            Assert.IsNotNull(manager.TickModules);
        }

        [Test]
        public void ModuleCatalog_Register_AddsModuleDefinitionOnce()
        {
            var sortedTypes = ModuleManager.GetRegisteredModuleTypes(new[]
            {
                typeof(UnregisteredTestModule),
                typeof(RegisteredLifecycleModule),
                typeof(EarlyRegisteredModule),
                typeof(SamePriorityRegisteredModule),
            }, includeTestAssemblies: true).ToList();

            Assert.That(sortedTypes, Has.Count.EqualTo(3));
            Assert.IsFalse(sortedTypes.Contains(typeof(UnregisteredTestModule)));
        }

        [Test]
        public void ModuleManager_IgnoresRegisteredTestModules()
        {
            var manager = CreateManager("ModuleManager_GetModule_Test");

            Assert.Throws<InvalidOperationException>(() => manager.GetModule<RegisteredLifecycleModule>());
        }

        [Test]
        public void ModuleManager_LifecycleLists_IgnoreRegisteredTestModules()
        {
            var manager = CreateManager("ModuleManager_Lifecycle_Test");

            Assert.IsFalse(manager.StartModules.Any(module => module is RegisteredLifecycleModule));
            Assert.IsFalse(manager.TickModules.Any(module => module is RegisteredLifecycleModule));
            Assert.IsFalse(manager.LateTickModules.Any(module => module is RegisteredLifecycleModule));
        }

        [Test]
        public void ModuleManager_GetRegisteredModuleTypes_SortsByPriorityThenName()
        {
            var sortedTypes = ModuleManager.GetRegisteredModuleTypes(new[]
            {
                typeof(RegisteredLifecycleModule),
                typeof(SamePriorityRegisteredModule),
                typeof(EarlyRegisteredModule),
            }, includeTestAssemblies: true).ToList();

            Assert.That(sortedTypes[0], Is.EqualTo(typeof(EarlyRegisteredModule)));
            Assert.That(sortedTypes[1], Is.EqualTo(typeof(SamePriorityRegisteredModule)));
            Assert.That(sortedTypes[2], Is.EqualTo(typeof(RegisteredLifecycleModule)));
        }

        [Test]
        public void ModuleManager_GetModule_ForUnregisteredType_Throws()
        {
            var manager = CreateManager("ModuleManager_GetModule_Throws_Test");

            Assert.Throws<InvalidOperationException>(() => manager.GetModule<UnregisteredTestModule>());
        }
    }
}
