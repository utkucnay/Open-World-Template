using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Scripting;

namespace Glai.Module
{
    public readonly struct RuntimeModuleDefinition
    {
        public RuntimeModuleDefinition(Type moduleType, Func<ModuleBase> create)
        {
            ModuleType = moduleType ?? throw new ArgumentNullException(nameof(moduleType));
            Create = create ?? throw new ArgumentNullException(nameof(create));
        }

        public Type ModuleType { get; }

        public Func<ModuleBase> Create { get; }
    }

    public static class RuntimeModuleCatalog
    {
        private static readonly List<RuntimeModuleDefinition> definitions = new List<RuntimeModuleDefinition>();
        private static readonly HashSet<Type> registeredTypes = new HashSet<Type>();

        public static IReadOnlyList<RuntimeModuleDefinition> Definitions => definitions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            definitions.Clear();
            registeredTypes.Clear();
        }

        internal static void ResetForTests()
        {
            Reset();
        }

        public static void Register<T>() where T : ModuleBase, new()
        {
            Register(typeof(T), static () => new T());
        }

        public static void Register(Type moduleType, Func<ModuleBase> create)
        {
            if (moduleType == null)
            {
                throw new ArgumentNullException(nameof(moduleType));
            }

            if (create == null)
            {
                throw new ArgumentNullException(nameof(create));
            }

            if (!typeof(ModuleBase).IsAssignableFrom(moduleType))
            {
                throw new ArgumentException($"{moduleType.FullName} must inherit from {nameof(ModuleBase)}.", nameof(moduleType));
            }

            if (!registeredTypes.Add(moduleType))
            {
                return;
            }

            definitions.Add(new RuntimeModuleDefinition(moduleType, create));
        }
    }

    [Preserve]
    public class ModuleManager : MonoBehaviour
    {
        public Dictionary<System.Type, ModuleBase> Modules { get; private set; } = new Dictionary<System.Type, ModuleBase>();
        public List<IStart> StartModules { get; private set; } = new List<IStart>();
        public List<ITick> TickModules { get; private set; } = new List<ITick>();
        public List<ILateTick> LateTickModules { get; private set; } = new List<ILateTick>();
        public static ModuleManager Instance { get; private set; }

        private bool _isInitialized = false;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad), Preserve]
        public static void CreateOnInitialize()
        {
            if (Instance != null)
            {
                return;
            }

            Debug.developerConsoleVisible = true;
            Debug.Log($"[ModuleManager] Creating ModuleManager on initialize.");

            var gameObject = new GameObject("ModuleManager");
            gameObject.AddComponent<ModuleManager>();

            
            var defaultLoop = PlayerLoop.GetDefaultPlayerLoop();

            var newLoop = new PlayerLoopSystem
            {
                type = typeof(PlayerLoop),
                subSystemList = new PlayerLoopSystem[]
                {
                    new PlayerLoopSystem
                    {
                        type = typeof(TimeUpdate),
                        subSystemList = new PlayerLoopSystem[]
                        {
                            FindSystem(typeof(TimeUpdate.WaitForLastPresentationAndUpdateTime), defaultLoop)
                        }
                    },

                    new PlayerLoopSystem
                    {
                        type = typeof(Initialization),
                        subSystemList = new PlayerLoopSystem[]
                        {
                            FindSystem(typeof(Initialization.ProfilerStartFrame), defaultLoop),
                            FindSystem(typeof(Initialization.UpdateCameraMotionVectors), defaultLoop)
                        }
                    },

                    new PlayerLoopSystem
                    {
                        type = typeof(EarlyUpdate),
                        subSystemList = new PlayerLoopSystem[]
                        {
                            FindSystem(typeof(EarlyUpdate.GpuTimestamp), defaultLoop),
                            FindSystem(typeof(EarlyUpdate.ExecuteMainThreadJobs), defaultLoop),
                            FindSystem(typeof(EarlyUpdate.ClearIntermediateRenderers), defaultLoop),
                            FindSystem(typeof(EarlyUpdate.ClearLines), defaultLoop),
                            FindSystem(typeof(EarlyUpdate.PresentBeforeUpdate), defaultLoop),
                            FindSystem(typeof(EarlyUpdate.RendererNotifyInvisible), defaultLoop),
                            FindSystem(typeof(EarlyUpdate.UpdateMainGameViewRect), defaultLoop),
                            FindSystem(typeof(EarlyUpdate.UpdateInputManager), defaultLoop)
                        }
                    },

                    new PlayerLoopSystem
                    {
                        type = typeof(FixedUpdate),
                        subSystemList = new PlayerLoopSystem[]
                        {
                            FindSystem(typeof(FixedUpdate.AudioFixedUpdate), defaultLoop),
                            FindSystem(typeof(FixedUpdate.ScriptRunBehaviourFixedUpdate), defaultLoop),
                        },
                    },

                    new PlayerLoopSystem
                    {
                        type = typeof(Update),
                        subSystemList = new PlayerLoopSystem[]
                        {
                            FindSystem(typeof(Update.ScriptRunBehaviourUpdate), defaultLoop)
                        },
                    },

                    new PlayerLoopSystem
                    {
                        type = typeof(PreLateUpdate),
                        subSystemList = new PlayerLoopSystem[]
                        {
                            FindSystem(typeof(PreLateUpdate.EndGraphicsJobsAfterScriptUpdate), defaultLoop),
                            FindSystem(typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate), defaultLoop),
                        },
                    },

                    new PlayerLoopSystem
                    {
                        type = typeof(PostLateUpdate),
                        subSystemList = new PlayerLoopSystem[]
                        {
                            FindSystem(typeof(PostLateUpdate.UpdateAudio), defaultLoop),
                            FindSystem(typeof(PostLateUpdate.EndGraphicsJobsAfterScriptLateUpdate), defaultLoop),
                            FindSystem(typeof(PostLateUpdate.UpdateCustomRenderTextures), defaultLoop),
                            FindSystem(typeof(PostLateUpdate.UpdateAllRenderers), defaultLoop),
                            FindSystem(typeof(PostLateUpdate.UpdateLightProbeProxyVolumes), defaultLoop),
                            FindSystem(typeof(PostLateUpdate.PresentAfterDraw), defaultLoop),
                            FindSystem(typeof(PostLateUpdate.FinishFrameRendering), defaultLoop),
                            FindSystem(typeof(PostLateUpdate.ClearImmediateRenderers), defaultLoop),
                            FindSystem(typeof(PostLateUpdate.UpdateResolution), defaultLoop),
                            FindSystem(typeof(PostLateUpdate.InputEndFrame), defaultLoop),
                            FindSystem(typeof(PostLateUpdate.ShaderHandleErrors), defaultLoop),
                            FindSystem(typeof(PostLateUpdate.ResetInputAxis), defaultLoop),
                            FindSystem(typeof(PostLateUpdate.MemoryFrameMaintenance), defaultLoop),
                            FindSystem(typeof(PostLateUpdate.GraphicsWarmupPreloadedShaders), defaultLoop),
                            //FindSystem(typeof(PostLateUpdate.TriggerEndOfFrameCallbacks), defaultLoop),
                            //FindSystem(typeof(PostLateUpdate.ObjectDispatcherPostLateUpdate), defaultLoop),
                        },
                    },
                }
            };

            PlayerLoop.SetPlayerLoop(newLoop);
        }

        static PlayerLoopSystem FindSystem(Type type, PlayerLoopSystem root)
        {
            if (root.type == type)
                return root;

            if (root.subSystemList == null)
                return default;

            foreach (var sub in root.subSystemList)
            {
                var found = FindSystem(type, sub);
                if (found.type != null)
                    return found;
            }

            return default;
        }

        private static void RegisterModuleLifecycle(ModuleBase module, List<IStart> startModules, List<ITick> tickModules, List<ILateTick> lateTickModules)
        {
            if (module is IStart startModule)
            {
                startModules.Add(startModule);
            }

            if (module is ITick tickModule)
            {
                tickModules.Add(tickModule);
            }

            if (module is ILateTick lateTickModule)
            {
                lateTickModules.Add(lateTickModule);
            }
        }

        private void RegisterModules()
        {
            var definitions = RuntimeModuleCatalog.Definitions;

            Debug.developerConsoleVisible = true;
            Debug.Log($"[ModuleManager] Registering {definitions.Count} modules.");

            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                var module = definition.Create();

                Modules.Add(definition.ModuleType, module);
                module.Initialize();
                RegisterModuleLifecycle(module, StartModules, TickModules, LateTickModules);
            }
        }

        private void DisposeModules()
        {
            foreach (var module in Modules.Values)
            {
                module.Dispose();
            }

            Modules.Clear();
            StartModules.Clear();
            TickModules.Clear();
            LateTickModules.Clear();
            _isInitialized = false;
        }

        private void Awake()
        {
            if ( Instance != null && Instance != this)
            {
                Debug.developerConsoleVisible = true;
                Debug.Log($"[ModuleManager] Found existing instance of ModuleManager. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            Modules.Clear();
            StartModules.Clear();
            TickModules.Clear();
            LateTickModules.Clear();

            RegisterModules();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                DisposeModules();
                Instance = null;
            }
        }

        void Start()
        {
            if (!_isInitialized)
            {
                for (int i = 0; i < StartModules.Count; i++)
                {
                    StartModules[i].Start();
                }

                Debug.Log($"[ModuleManager] Initialized with {Modules.Count} modules, {StartModules.Count} start modules, {TickModules.Count} tick modules, {LateTickModules.Count} late tick modules.");
                _isInitialized = true;
            }
        }

        void Update()
        {
            if (!_isInitialized)
            {
                for (int i = 0; i < StartModules.Count; i++)
                {
                    StartModules[i].Start();
                }

                _isInitialized = true;
            }

            for (int i = 0; i < TickModules.Count; i++)
            {
                TickModules[i].Tick(Time.deltaTime);
            }
        }

        void LateUpdate()
        {
            for (int i = 0; i < LateTickModules.Count; i++)
            {
                LateTickModules[i].LateTick(Time.deltaTime);
            }
        }

        public T GetModule<T>() where T : ModuleBase
        {
            if (Modules.TryGetValue(typeof(T), out var module))
            {
                return module as T;
            }

            throw new InvalidOperationException($"Module of type {typeof(T).Name} is not registered.");
        }
    }
}
