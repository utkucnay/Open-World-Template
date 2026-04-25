using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Glai.Core
{
    public unsafe static class AllocatorHelper
    {
        public static int ResolveAlignment<T>(int alignment) where T : unmanaged
        {
            if (alignment == 0)
            {
                alignment = UnsafeUtility.AlignOf<T>();
            }

            if (!IsPowerOfTwo(alignment))
            {
                throw new ArgumentException("Alignment must be a positive power of two.", nameof(alignment));
            }

            return alignment;
        }

        public static int AlignForward(int offset, int alignment)
        {
            int mask = alignment - 1;
            return (offset + mask) & ~mask;
        }

        public static byte* AlignForward(byte* pointer, int alignment)
        {
            ulong value = (ulong)pointer;
            ulong mask = (ulong)(alignment - 1);
            return (byte*)((value + mask) & ~mask);
        }

        public static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }
    }
}
