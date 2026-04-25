using System.Runtime.CompilerServices;

namespace Glai.ECS
{
    public unsafe readonly struct BufferRW4<T> where T : unmanaged, IBufferComponent
    {
        readonly byte* slotsBase;
        readonly int capacity;
        readonly int slotSize;

        public const int Length = 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BufferRW4(void* slotsBase, int capacity)
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

        public BufferRW<T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new BufferRW<T>(slotsBase + index * slotSize, capacity);
        }
    }
}
