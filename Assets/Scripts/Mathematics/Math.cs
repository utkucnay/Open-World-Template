using System.Runtime.CompilerServices;

namespace Glai.Mathematics
{
    public static class Math
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long GB(int value)
        {
            return value * 1024L * 1024L * 1024L;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MB(int value)
        {
            return value * 1024 * 1024;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int KB(int value)
        {
            return value * 1024;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int B(int value)
        {
            return value;
        }
    }
}
