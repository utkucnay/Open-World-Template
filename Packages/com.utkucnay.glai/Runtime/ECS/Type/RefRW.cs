using System.Runtime.CompilerServices;

namespace Glai.ECS
{
    public unsafe readonly struct RefRW<T> where T : unmanaged, IComponent
    {
        readonly T* ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefRW(T* ptr)
        {
            this.ptr = ptr;
        }

        public ref T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref *ptr;
        }

        public T* Ptr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ptr;
        }
    }
}
