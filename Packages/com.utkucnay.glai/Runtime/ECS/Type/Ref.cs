using System.Runtime.CompilerServices;

namespace Glai.ECS
{
    public unsafe readonly struct Ref<T> where T : unmanaged, IComponent
    {
        readonly T* ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ref(T* ptr)
        {
            this.ptr = ptr;
        }

        public ref T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref *ptr;
        }

        public T* ValueT
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ptr;
        }
    }
}
