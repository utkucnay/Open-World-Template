using Unity.Burst;
using static Unity.Burst.Intrinsics.X86;

namespace Glai.Core
{
    public enum SIMDInstructionSet
    {
        None,
        SSE,
        SSE2,
        SSE3,
        SSSE3,
        SSE4_1,
        SSE4_2,
        AVX,
        AVX2
    }

    public static class SIMDHelper
    {
        public static int GetDefaultAlignment()
        {
            if (Avx2.IsAvx2Supported) return 32;
            if (Avx.IsAvxSupported) return 32;
            if (Sse4_2.IsSse42Supported) return 16;
            if (Sse4_1.IsSse41Supported) return 16;
            if (Ssse3.IsSsse3Supported) return 16;
            if (Sse3.IsSse3Supported) return 16;
            if (Sse2.IsSse2Supported) return 16;
            if (Sse.IsSseSupported) return 16;
            return 16; // Default to 16-byte alignment for older architectures
        }

        public static SIMDInstructionSet GetSupportedInstructionSet()
        {
            if (Avx2.IsAvx2Supported) return SIMDInstructionSet.AVX2;
            if (Avx.IsAvxSupported) return SIMDInstructionSet.AVX;
            if (Sse4_2.IsSse42Supported) return SIMDInstructionSet.SSE4_2;
            if (Sse4_1.IsSse41Supported) return SIMDInstructionSet.SSE4_1;
            if (Ssse3.IsSsse3Supported) return SIMDInstructionSet.SSSE3;
            if (Sse3.IsSse3Supported) return SIMDInstructionSet.SSE3;
            if (Sse2.IsSse2Supported) return SIMDInstructionSet.SSE2;
            if (Sse.IsSseSupported) return SIMDInstructionSet.SSE;
            return SIMDInstructionSet.None; // No supported instruction set found
        }
    }
}