using System;
using Glai.Core;
using Unity.Collections.LowLevel.Unsafe;

namespace Glai.Collections
{
    public unsafe struct FixedString128Bytes
    {
        public fixed char data[128];
        public int Length { get; private set; }
        public int Capacity => 128;

        public FixedString128Bytes(string value)
        {
            if (value.Length > 128)
            {
                Logger.LogWarning($"String length exceeds 128 characters. Truncating to 128 characters. string: {value}");
                Length = 0;
                return;
            }

            fixed (char* ptr = data)
            fixed (char* valuePtr = value)
            {
                UnsafeUtility.MemCpy((void*)valuePtr, (void*)ptr, value.Length * sizeof(char));
                Length = value.Length;
            }
        }

        public override string ToString()
        {
            fixed (char* ptr = data)
            {
                return new string(ptr, 0, Length);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is FixedString128Bytes other)
            {
                return this == other;
            }

            if (obj is string str)
            {
                return this == str;
            }

            return false;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            for (int i = 0; i < Length; i++)
            {
                hash = hash * 31 + this[i];
            }
            return hash;
        }

        public static implicit operator FixedString128Bytes(string value)
        {
            return new FixedString128Bytes(value);
        }
        
        public static implicit operator string(FixedString128Bytes fixedString)
        {
            return fixedString.ToString();
        }

        public static bool operator ==(FixedString128Bytes a, FixedString128Bytes b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a.data[i] != b.data[i])
                    return false;
            }

            return true;
        }

        public static bool operator !=(FixedString128Bytes a, FixedString128Bytes b)
        {
            return !(a == b);
        }

        public static bool operator ==(FixedString128Bytes a, string b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a.data[i] != b[i])
                    return false;
            }

            return true;
        }

        public static bool operator !=(FixedString128Bytes a, string b)
        {
            return !(a == b);
        }

        public byte this[int index]
        {
            get
            {
                if (index < 0 || index >= 128)
                {
                    throw new IndexOutOfRangeException($"Index {index} is out of range for FixedStrings128Bytes.");
                }

                fixed (char* ptr = data)
                {
                    return (byte)ptr[index];
                }
            }
            set
            {
                if (index < 0 || index >= 128)
                {
                    throw new IndexOutOfRangeException($"Index {index} is out of range for FixedStrings128Bytes.");
                }

                fixed (char* ptr = data)
                {
                    ptr[index] = (char)value;
                }
            }
        }
    }
}
