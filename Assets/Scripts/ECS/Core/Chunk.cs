using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Glai.Allocator;
using Glai.Collection;
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

    public struct Chunk : IEquatable<Chunk>, IDisposable
    {
        FixedString128Bytes name;

        int maxComponentSize;
        int componentCount;
        int capacityBytes;
        int nextComponentIndex;
        int entityCapacity;
        int componentRegionBytes;
        FixedStack<int> freeComponentIndices;

        IntPtr dataPtr;

        bool IsDisposed => dataPtr == IntPtr.Zero;

        public Chunk(in ChunkData data, MemoryStateHandle memoryStateHandle, MemoryState memoryState)
        {
            name = data.name;
            componentCount = data.componentCount;
            maxComponentSize = data.maxComponentSize;
            capacityBytes = data.capacityBytes;
            nextComponentIndex = 0;
            componentRegionBytes = capacityBytes / componentCount;
            entityCapacity = componentRegionBytes / maxComponentSize;
            
            if (entityCapacity <= 0)
            {
                throw new ArgumentException($"Chunk {name} has invalid capacity. capacityBytes={capacityBytes}, componentCount={componentCount}, maxComponentSize={maxComponentSize}.");
            }

            freeComponentIndices = new FixedStack<int>(entityCapacity, memoryStateHandle, memoryState);
            dataPtr = IntPtr.Zero;
            Allocate();
        }

        public void Dispose(MemoryState memoryState)
        {
            freeComponentIndices.Dispose(memoryState);
            Dispose();
        }

        public unsafe void Allocate()
        {
            if (!IsDisposed)
            {
                Logger.LogWarning($"Chunk {name} is already allocated.");
                return;
            }
            dataPtr = Marshal.AllocHGlobal(capacityBytes);
            Unsafe.InitBlock(dataPtr.ToPointer(), 0, (uint)capacityBytes);
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
        }

        public int CreateComponentIndex()
        {
            if (freeComponentIndices.Count > 0)
            {
                return freeComponentIndices.Pop();
            }
            else
            {
                if (nextComponentIndex >= entityCapacity)
                {
                    throw new InvalidOperationException($"Chunk {name} is full.");
                }
                return nextComponentIndex++;
             }
        }

        public void RemoveComponentIndex(int index)
        {
            if (index < 0 || index >= nextComponentIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Invalid component index {index} for chunk {name}.");
            }
            
            freeComponentIndices.Push(index);
        }

        public unsafe Span<T> GetComponents<T>(int componentIndex) where T : unmanaged
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException($"Chunk {name} is not allocated.");
            }

            if (componentIndex < 0 || componentIndex >= componentCount)
            {
                throw new ArgumentOutOfRangeException(nameof(componentIndex), $"Invalid component index {componentIndex} for chunk {name}.");
            }

            return new Span<T>((void*)(dataPtr + (componentIndex * componentRegionBytes)), entityCapacity);
        }

        public unsafe ref T GetComponent<T>(int componentIndex, int index) where T : unmanaged
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

            return ref Unsafe.AsRef<T>((void*)(dataPtr + (componentIndex * componentRegionBytes) + (index * sizeof(T))));
        }

        public bool Equals(Chunk other)
        {
            return dataPtr == other.dataPtr;
        }

        public bool IsFull()
        {
            return nextComponentIndex >= entityCapacity && freeComponentIndices.Count == 0;
        }
    }
}
