using System;

namespace Glai.Core
{
    public enum ByteUnit
    {
        B,
        KB,
        MB,
        GB,
    }

    [Serializable]
    public struct ByteSize
    {
        public int Value;
        public ByteUnit Unit;

        public int Bytes
        {
            get
            {
                long bytes = (long)Value * GetMultiplier(Unit);
                if (bytes > int.MaxValue)
                {
                    throw new OverflowException($"Byte size {Value} {Unit} exceeds Int32.MaxValue bytes.");
                }

                return (int)bytes;
            }
        }

        public static ByteSize B(int value) => new ByteSize { Value = value, Unit = ByteUnit.B };
        public static ByteSize KB(int value) => new ByteSize { Value = value, Unit = ByteUnit.KB };
        public static ByteSize MB(int value) => new ByteSize { Value = value, Unit = ByteUnit.MB };
        public static ByteSize GB(int value) => new ByteSize { Value = value, Unit = ByteUnit.GB };

        private static long GetMultiplier(ByteUnit unit)
        {
            switch (unit)
            {
                case ByteUnit.B:
                    return 1L;
                case ByteUnit.KB:
                    return 1024L;
                case ByteUnit.MB:
                    return 1024L * 1024L;
                case ByteUnit.GB:
                    return 1024L * 1024L * 1024L;
                default:
                    return 1L;
            }
        }
    }
}
