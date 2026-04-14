using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Glai.Allocator;
using Glai.Core;
using Unity.Collections;

namespace Glai.ECS.Core
{
    public ref struct ChunkData
    {
        public FixedString128Bytes name;
        public int capacityBytes;
        public int maxComponentSize;
        public int componentCount;
    }

    public unsafe struct Chunk : IEquatable<Chunk>, IDisposable
    {
        FixedString128Bytes name;

        int maxComponentSize;
        int componentCount;
        int capacityBytes;
        int entityCount;
        int entityCapacity;
        int componentRegionBytes;

        IntPtr dataPtr;
        int* entityIds;

        bool IsDisposed => dataPtr == IntPtr.Zero;

        public int EntityCount => entityCount;
        public int EntityCapacity => entityCapacity;
        public int ComponentRegionBytes => componentRegionBytes;
        public int MaxComponentSize => maxComponentSize;
        public IntPtr DataPtr => dataPtr;
        public int* EntityIds => entityIds;

        public Chunk(in ChunkData data, MemoryStateHandle memoryStateHandle, MemoryState memoryState)
        {
            name = data.name;
            componentCount = data.componentCount;
            maxComponentSize = data.maxComponentSize;
            capacityBytes = data.capacityBytes;
            entityCount = 0;
            componentRegionBytes = capacityBytes / componentCount;
            entityCapacity = componentRegionBytes / maxComponentSize;
            
            if (entityCapacity <= 0)
            {
                throw new ArgumentException($"Chunk {name} has invalid capacity. capacityBytes={capacityBytes}, componentCount={componentCount}, maxComponentSize={maxComponentSize}.");
            }

            entityIds = (int*)Marshal.AllocHGlobal(entityCapacity * sizeof(int));
            Unsafe.InitBlock(entityIds, 0, (uint)(entityCapacity * sizeof(int)));

            dataPtr = IntPtr.Zero;
            Allocate();
        }

        public void Dispose()
        {   
            if (IsDisposed)
            {
                Logger.LogWarning($"Chunk {name} is already disposed.");
                return;
            }
            Marshal.FreeHGlobal(dataPtr);
            dataPtr = IntPtr.Zero;

            if (entityIds != null)
            {
                Marshal.FreeHGlobal((IntPtr)entityIds);
                entityIds = null;
            }
        }

        public void Allocate()
        {
            if (!IsDisposed)
            {
                Logger.LogWarning($"Chunk {name} is already allocated.");
                return;
            }
            dataPtr = Marshal.AllocHGlobal(capacityBytes);
            Unsafe.InitBlock(dataPtr.ToPointer(), 0, (uint)capacityBytes);
        }

        public int CreateSlot(int entityId)
        {
            if (entityCount >= entityCapacity)
            {
                throw new InvalidOperationException($"Chunk {name} is full.");
            }

            int index = entityCount;
            entityIds[index] = entityId;
            entityCount++;
            return index;
        }

        public int RemoveSlot(int index)
        {
            if (index < 0 || index >= entityCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Invalid slot index {index} for chunk {name}.");
            }

            int lastIndex = entityCount - 1;
            int swappedEntityId = -1;

            if (index != lastIndex)
            {
                for (int c = 0; c < componentCount; c++)
                {
                    byte* regionBase = (byte*)dataPtr + c * componentRegionBytes;
                    Unsafe.CopyBlock(
                        regionBase + index * maxComponentSize,
                        regionBase + lastIndex * maxComponentSize,
                        (uint)maxComponentSize);
                }

                swappedEntityId = entityIds[lastIndex];
                entityIds[index] = swappedEntityId;
            }

            entityCount--;
            return swappedEntityId;
        }

        public ref T GetComponent<T>(int componentIndex, int index) where T : unmanaged
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException($"Chunk {name} is not allocated.");
            }

            if (componentIndex < 0 || componentIndex >= componentCount)
            {
                throw new ArgumentOutOfRangeException(nameof(componentIndex), $"Invalid component index {componentIndex} for chunk {name}.");
            }

            if (index < 0 || index >= entityCapacity)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Invalid entity index {index} for chunk {name}.");
            }

            return ref Unsafe.AsRef<T>((void*)(dataPtr + (componentIndex * componentRegionBytes) + (index * maxComponentSize)));
        }

        public bool Equals(Chunk other)
        {
            return dataPtr == other.dataPtr;
        }

        public bool IsFull()
        {
            return entityCount >= entityCapacity;
        }
    }
}
