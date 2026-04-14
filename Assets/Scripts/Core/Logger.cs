using System.Runtime.CompilerServices;
using UnityEngine;

namespace Glai.Core
{
    public static class Logger
    {
        public static bool EnableLog { get; set; } = true;
        public static bool EnableWarning { get; set; } = true;
        public static bool EnableError { get; set; } = true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetChannels()
        {
            EnableLog = true;
            EnableWarning = true;
            EnableError = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(string message)
        {
            if (!EnableLog) return;
            Debug.Log(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarning(string message)
        {
            if (!EnableWarning) return;
            Debug.LogWarning(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(string message)
        {
            if (!EnableError) return;
            Debug.LogError(message);
        }    
    }
}
