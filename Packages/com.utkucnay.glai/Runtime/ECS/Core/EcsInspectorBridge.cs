using System;
using System.Runtime.InteropServices;
using Glai.Core;
using Glai.ECS.Core;

namespace Glai.ECS.Core
{
    public readonly struct EcsComponentSnapshot
    {
        public readonly int TypeId;
        public readonly string TypeName;
        public readonly bool IsTag;
        public readonly bool IsBuffer;
        public readonly object BoxedValue;

        public EcsComponentSnapshot(int typeId, string typeName, bool isTag, bool isBuffer, object boxedValue)
        {
            TypeId = typeId;
            TypeName = typeName;
            IsTag = isTag;
            IsBuffer = isBuffer;
            BoxedValue = boxedValue;
        }
    }

    public static unsafe class EcsInspectorBridge
    {
        public static EcsComponentSnapshot[] GetComponentSnapshots(ref Archetype archetype, ref Chunk chunk, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= chunk.EntityCount)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            EcsComponentSnapshot[] snapshots = new EcsComponentSnapshot[archetype.ComponentCount];
            for (int i = 0; i < archetype.ComponentCount; i++)
            {
                int typeId = archetype.GetComponentTypeId(i);
                Type componentType = TypeRegistry.GetType(typeId);
                string typeName = TypeRegistry.GetTypeName(typeId);
                int storageIndex = archetype.GetComponentStorageIndex(typeId);
                bool isTag = storageIndex < 0;
                bool isBuffer = componentType != null && typeof(IBufferComponent).IsAssignableFrom(componentType);

                object boxedValue = null;
                if (isTag)
                {
                    boxedValue = "Tag component";
                }
                else if (isBuffer)
                {
                    int slotSize = chunk.GetComponentSize(storageIndex);
                    boxedValue = $"Buffer component ({typeName}) slotSize={slotSize}";
                }
                else if (componentType != null)
                {
                    int componentSize = chunk.GetComponentSize(storageIndex);
                    byte* componentPtr = chunk.GetComponentPtr(storageIndex) + slotIndex * componentSize;
                    boxedValue = Marshal.PtrToStructure((IntPtr)componentPtr, componentType);
                }

                snapshots[i] = new EcsComponentSnapshot(typeId, typeName, isTag, isBuffer, boxedValue);
            }

            return snapshots;
        }
    }
}
