using System.Runtime.CompilerServices;

namespace Glai.ECS
{
    public unsafe readonly struct EntityId8
    {
        readonly int* ptr;

        public const int Length = 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityId8(int* ptr)
        {
            this.ptr = ptr;
        }

        public int* Ptr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ptr;
        }

        public int this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ptr[index];
        }
    }
}
