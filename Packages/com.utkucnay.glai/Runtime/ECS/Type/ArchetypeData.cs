using System;
using System.Runtime.CompilerServices;
using Glai.Core;
using Glai.ECS.Core;

namespace Glai.ECS
{
    internal ref struct ArchetypeData
    {
        public Span<int> ComponentTypeIds;
        public Span<int> ComponentSizes;
        private int componentTypeIndex;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeData(Span<int> componentTypeIds, Span<int> componentSizes)
        {
            ComponentTypeIds = componentTypeIds;
            ComponentSizes = componentSizes;
            componentTypeIndex = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void AddComponent<T>() where T : unmanaged, IComponent
        {
            int componentTypeId = TypeId<T>.Id;
            AddType(componentTypeId, sizeof(T), $"Duplicate component type {typeof(T)} in archetype.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBufferComponent<T>() where T : unmanaged, IBufferComponent
        {
            int componentTypeId = TypeId<T>.Id;
            AddType(componentTypeId, BufferTypeMetadata<T>.SlotSize, $"Duplicate buffer component type {typeof(T)} in archetype.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddType(int componentTypeId, int componentSize)
        {
            AddType(componentTypeId, componentSize, $"Duplicate component type id {componentTypeId} in archetype.");
        }

        void AddType(int componentTypeId, int componentSize, string duplicateMessage)
        {
            for (int i = 0; i < componentTypeIndex; i++)
            {
                if (ComponentTypeIds[i] == componentTypeId)
                {
                    throw new InvalidOperationException(duplicateMessage);
                }
            }

            ComponentTypeIds[componentTypeIndex] = componentTypeId;
            ComponentSizes[componentTypeIndex] = componentSize;
            componentTypeIndex++;
        }

        public int ComponentCount => componentTypeIndex;
    }
}
