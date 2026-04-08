using System.Runtime.CompilerServices;
using UnityEngine;

namespace Glai.Core
{
    public static class Logger
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Log(string message)
        {
            Debug.Log(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogError(string message)
        {
            Debug.LogError(message);
        }    
    }
}