using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityEngine.Scripting;

namespace Glai.Module
{
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

            Global.Initialize();

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
                            FindSystem(typeof(EarlyUpdate.ScriptRunDelayedStartupFrame), defaultLoop),
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

        internal static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Debug.LogWarning($"[ModuleManager] Some types failed to load from {assembly.FullName}.");
                return ex.Types.Where(type => type != null);
            }
        }

        internal static IEnumerable<Type> GetRegisteredModuleTypes(IEnumerable<Type> types, bool includeTestAssemblies = false)
        {
            return types
                .Where(type => IsRegisteredModuleType(type, includeTestAssemblies))
                .OrderBy(type => type.GetCustomAttribute<ModuleRegisterAttribute>().Priority)
                .ThenBy(type => type.FullName);
        }

        static bool IsRegisteredModuleType(Type type, bool includeTestAssemblies)
        {
            if (!typeof(ModuleBase).IsAssignableFrom(type) || type.IsAbstract || type.GetCustomAttribute<ModuleRegisterAttribute>() == null)
            {
                return false;
            }

            return includeTestAssemblies || !type.Assembly.GetName().Name.Contains(".Tests.");
        }

        private void RegisterModules()
        {
            // Find all non-abstract classes that inherit from ModuleBase and have the ModuleRegisterAttribute, then create an instance and register them.
            var types = GetRegisteredModuleTypes(AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(GetLoadableTypes))
                .ToList();

            foreach (var type in types)
            {
                var module = Activator.CreateInstance(type) as ModuleBase;
                Modules[type] = module;
                RegisterModuleLifecycle(module, StartModules, TickModules, LateTickModules);
            }

            foreach (var module in Modules.Values)
            {
                module.Initialize();
            }
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

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            foreach (var module in Modules.Values)
            {
                module.Dispose();
            }

            Modules.Clear();
            StartModules.Clear();
            TickModules.Clear();
            LateTickModules.Clear();

            Global.Dispose();
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
