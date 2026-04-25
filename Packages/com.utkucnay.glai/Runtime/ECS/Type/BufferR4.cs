using System.Runtime.CompilerServices;

namespace Glai.ECS
{
    public unsafe readonly struct BufferR4<T> where T : unmanaged, IBufferComponent
    {
        readonly byte* slotsBase;
        readonly int capacity;
        readonly int slotSize;

        public const int Length = 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferR4(void* slotsBase, int capacity)
        {
            this.slotsBase = (byte*)slotsBase;
            this.capacity = capacity;
            slotSize = Core.BufferTypeInfo<T>.HeaderSize + capacity * sizeof(T);
        }

        public byte* Ptr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => slotsBase;
        }

        public BufferR<T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new BufferR<T>(slotsBase + index * slotSize, capacity);
        }
    }
}
