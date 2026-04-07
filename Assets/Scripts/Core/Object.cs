using System;

namespace Glai.Core
{
    public abstract class Object : IDisposable
    {
        private enum ObjectState
        {
            Creation,
            Live,
            Destruction
        }

        public Guid Id { get; private set; }
        private string Name => GetType().Name;
        private ObjectState state;

        public bool Disposed { get; private set; }

        public Object()
        {
            Id = Guid.NewGuid();
            
            state = ObjectState.Creation;
            Log("Object created.");
            
            state = ObjectState.Live;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

#if UNITY_EDITOR
        public void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                Dispose();
                UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            }
        }
#endif

        ~Object()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            Disposed = true;
            state = ObjectState.Destruction;
            Log("Object disposed.");
        }

        protected void Log(string message)
        {
            Logger.Log($"[{Name} - {Id} - {state}] {message}");
        }

        protected void LogWarning(string message)
        {
            Logger.LogWarning($"[{Name} - {Id} - {state}] {message}");
        }

        protected void LogError(string message, bool closeApplication = false)
        {
            string logMessage = $"[{Name} - {Id} - {state}] {message}";
            Logger.LogError(logMessage);

            if (closeApplication)
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
    }
}