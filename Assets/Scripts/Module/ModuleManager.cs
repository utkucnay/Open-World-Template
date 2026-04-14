using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Glai.Module
{
    public class ModuleManager : MonoBehaviour
    {
        public Dictionary<System.Type, ModuleBase> Modules { get; private set; } = new Dictionary<System.Type, ModuleBase>();
        public List<IStart> StartModules { get; private set; } = new List<IStart>();
        public List<ITick> TickModules { get; private set; } = new List<ITick>();
        public static ModuleManager Instance { get; private set; }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void CreateOnInitialize()
        {
            var gameObject = new GameObject("ModuleManager");
            gameObject.AddComponent<ModuleManager>();
        }

        private void Awake()
        {
            if ( Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            Instance = this;
            Modules.Clear();
            StartModules.Clear();
            TickModules.Clear();

            var moduleTypes = System.AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && !type.IsAbstract && typeof(ModuleBase).IsAssignableFrom(type) && type.GetCustomAttributes(typeof(ModuleRegisterAttribute), false).Length > 0);
            
            foreach (var moduleType in moduleTypes)
            {                
                var module = (ModuleBase)System.Activator.CreateInstance(moduleType);
                Modules.Add(moduleType, module);
                module.Initialize();

                if (module is IStart startModule)
                {
                    StartModules.Add(startModule);
                }

                if (module is ITick tickModule)
                {
                    TickModules.Add(tickModule);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Start()
        {
            for (int i = 0; i < StartModules.Count; i++)
            {
                StartModules[i].Start();
            }
        }

        void Update()
        {
            for (int i = 0; i < TickModules.Count; i++)
            {
                TickModules[i].Tick(Time.deltaTime);
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
