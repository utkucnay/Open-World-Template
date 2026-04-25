using System.Runtime.CompilerServices;

namespace Glai.ECS
{
    public unsafe readonly struct RefR8<T> where T : unmanaged, IComponent
    {
        readonly T* ptr;

        public const int Length = 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefR8(T* ptr)
        {
            this.ptr = ptr;
        }

        public T* Ptr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ptr;
        }

        public ref readonly T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref ptr[index];
        }
    }
}
