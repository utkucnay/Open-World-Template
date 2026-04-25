using System.Runtime.CompilerServices;

namespace Glai.ECS
{
    public unsafe readonly struct EntityId4
    {
        readonly int* ptr;

        public const int Length = 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EntityId4(int* ptr)
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
