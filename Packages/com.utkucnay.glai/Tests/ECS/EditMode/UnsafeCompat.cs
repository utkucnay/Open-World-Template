using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    internal static unsafe class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* AsPointer<T>(ref T value) where T : unmanaged
        {
            fixed (T* ptr = &value)
            {
                return ptr;
            }
        }
    }
}
