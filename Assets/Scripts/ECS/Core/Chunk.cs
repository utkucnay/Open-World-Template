using System;
using System.Runtime.InteropServices;
using Glai.Allocator;
using Glai.Collection;
using Glai.Core;
using Glai.Mathematics;
using Unity.Collections;

namespace Glai.ECS.Core
{
    public struct ChunkData
    {
        public FixedString128Bytes name;
        public int capacityBytes;
        public FixedArray<int> bytesOfComponents;
    }

    public struct Chunk : IDisposable
    {
        FixedString128Bytes name;
        IntPtr dataPtr;
        int componentCount;
        int capacityBytes;

        bool IsDisposed => dataPtr == IntPtr.Zero;

        public Chunk(in ChunkData data)
        {
            name = data.name;
            componentCount = data.bytesOfComponents.Length;
            capacityBytes = data.capacityBytes;
            dataPtr = IntPtr.Zero;
        }

        public void Allocate()
        {
            if (!IsDisposed)
            {
                return;
            }
            dataPtr = Marshal.AllocHGlobal(capacityBytes);
        }

        public void Dispose()
        {   
            if (IsDisposed)
            {
                return;
            }
            Marshal.FreeHGlobal(dataPtr);
            dataPtr = IntPtr.Zero;
        }

        public Span<T> GetSpan<T>(int index) where T : unmanaged
        {
            if (index < 0 || index >= componentCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return Span<T>.Empty;
        }
    }
}
