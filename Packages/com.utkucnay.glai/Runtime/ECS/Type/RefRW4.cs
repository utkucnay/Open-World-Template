using System.Runtime.CompilerServices;

namespace Glai.ECS
{
    public unsafe readonly struct RefRW4<T> where T : unmanaged, IComponent
    {
        readonly T* ptr;

        public const int Length = 4;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefRW4(T* ptr)
        {
            this.ptr = ptr;
        }

        public T* Ptr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ptr;
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref ptr[index];
        }
    }
}
