using System.Runtime.CompilerServices;
using Glai.Core;
using Glai.ECS.Core;

namespace Glai.ECS
{
    public readonly struct ArchetypeType
    {
        readonly int typeId;
        readonly int size;

        ArchetypeType(int typeId, int size)
        {
            this.typeId = typeId;
            this.size = size;
        }

        internal int TypeId => typeId;
        internal int Size => size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe ArchetypeType Component<T>() where T : unmanaged, IComponent
        {
            return new ArchetypeType(TypeId<T>.Id, sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArchetypeType Buffer<T>() where T : unmanaged, IBufferComponent
        {
            return new ArchetypeType(TypeId<T>.Id, BufferTypeMetadata<T>.SlotSize);
        }
    }
}
