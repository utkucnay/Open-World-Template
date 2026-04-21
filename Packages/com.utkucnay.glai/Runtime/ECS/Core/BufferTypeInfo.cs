using System;
using Glai.ECS;

namespace Glai.ECS.Core
{
    public static class BufferTypeInfo<T> where T : unmanaged, IBufferComponent
    {
        public const int HeaderSize = sizeof(int);
        public static int Capacity => BufferTypeMetadata<T>.Capacity;
        public static int SlotSize => BufferTypeMetadata<T>.SlotSize;

        public static unsafe int GetSlotSize(int capacity)
        {
            return HeaderSize + capacity * sizeof(T);
        }

        public static unsafe int GetCapacity(int slotSize)
        {
            return (slotSize - HeaderSize) / sizeof(T);
        }
    }

    internal static class BufferTypeMetadata<T> where T : unmanaged, IBufferComponent
    {
        public static readonly int Capacity;
        public static readonly int SlotSize;

        static BufferTypeMetadata()
        {
            var attr = (FixedBufferAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(FixedBufferAttribute));
            if (attr == null)
                throw new InvalidOperationException(
                    $"Buffer component {typeof(T).FullName} is missing [FixedBuffer(capacity)] attribute.");
            if (attr.Capacity <= 0)
                throw new InvalidOperationException(
                    $"[FixedBuffer] capacity must be > 0 on {typeof(T).FullName}.");

            Capacity = attr.Capacity;
            SlotSize = BufferTypeInfo<T>.GetSlotSize(attr.Capacity);
        }
    }
}
