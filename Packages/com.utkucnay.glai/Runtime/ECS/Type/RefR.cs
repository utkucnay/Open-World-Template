using System.Runtime.CompilerServices;

namespace Glai.ECS
{
    public unsafe readonly struct RefR<T> where T : unmanaged, IComponent
    {
        readonly T* ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefR(T* ptr)
        {
            this.ptr = ptr;
        }

        public ref readonly T Value
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
