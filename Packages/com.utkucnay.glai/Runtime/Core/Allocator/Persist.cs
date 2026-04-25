using System;
using System.Runtime.InteropServices;
using Glai.Core;
using Glai.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Glai.Allocator
{
    public struct PersistData
    {
        public FixedString128Bytes name;
        public int alignmentBytes;
        public int capacityBytes;
        public int maxHandles;
    }

    public unsafe class Persist : Glai.Core.Object, IAllocator
    {
        IntPtr dataPtr;
        IntPtr offsetPtr;
        int dataCapacityBytes;

        Handle* handles;
        int maxHandle;
        int handleIndex;
        int peakCount;
        int peakHandleCount;

        public FixedString128Bytes Name { get; private set; }
        public int Count { get { return (int)((long)offsetPtr - (long)dataPtr) + (handleIndex * sizeof(Handle)); } }
        public int PeakCount { get { return peakCount; } }
        public int Capacity { get { return (int)dataCapacityBytes + (maxHandle * sizeof(Handle)); } }

        public int HandleCount { get { return handleIndex; } }
        public int PeakHandleCount { get { return peakHandleCount; } }
        public int HandleCapacity { get { return maxHandle; } }

        public bool IsDisposed { get; private set; }

        public Persist(in PersistData data) : base()
        {
            Name = data.name;

            var memoryPool = Global.DefaultPool;

            handles = (Handle*)memoryPool.Allocate(sizeof(Handle) * data.maxHandles, UnsafeUtility.AlignOf<Handle>());
            UnsafeUtility.MemClear(handles, (uint)(sizeof(Handle) * data.maxHandles));
            handleIndex = 0;
            maxHandle = data.maxHandles;
            peakHandleCount = 0;

            dataPtr = (IntPtr)memoryPool.Allocate(data.capacityBytes, data.alignmentBytes == 0 ? 16 : data.alignmentBytes);
            UnsafeUtility.MemClear(dataPtr.ToPointer(), (uint)data.capacityBytes);
            dataCapacityBytes = data.capacityBytes;
            offsetPtr = dataPtr;
            peakCount = 0;

            EventBus.Publish(IAllocator.RegisterEvent, this);
        }

        public override void Dispose()
        {
            if (IsDisposed || Disposed)
            {
                return;
            }

            base.Dispose();
            EventBus.Publish(IAllocator.UnregisterEvent, this);
            var memoryPool = Global.DefaultPool;
            memoryPool.Deallocate(dataPtr.ToPointer());
            memoryPool.Deallocate(handles);
            dataPtr = IntPtr.Zero;
            offsetPtr = IntPtr.Zero;
            handles = null;
            maxHandle = 0;
            peakCount = 0;
            peakHandleCount = 0;
            IsDisposed = true;
        }

        public Handle Allocate<T>(int alignment = 0) where T : unmanaged
        {
            int byteSize = sizeof(T);
            alignment = AllocatorHelper.ResolveAlignment<T>(alignment);
            int alignedOffset = (int)(AllocatorHelper.AlignForward((byte*)offsetPtr.ToPointer(), alignment) - (byte*)dataPtr.ToPointer());

            if (alignedOffset + byteSize > dataCapacityBytes)
            {
                throw new InvalidOperationException("Persist allocator is out of memory.");
            }

            if (handleIndex >= maxHandle)
            {
                throw new InvalidOperationException("Persist allocator has reached maximum handle count.");
            }

            var handle = new Handle(Id, handleIndex, alignedOffset, handles[handleIndex].Generation);
            handles[handleIndex] = handle;
            handleIndex++;
            offsetPtr = IntPtr.Add(dataPtr, alignedOffset + byteSize);
            UpdatePeaks();
            return handle;
        }

        public HandleArray AllocateArray<T>(int capacity, int alignment = 0) where T : unmanaged
        {
            int byteSize = sizeof(T);
            int totalBytes = byteSize * capacity;
            alignment = AllocatorHelper.ResolveAlignment<T>(alignment);
            int alignedOffset = (int)(AllocatorHelper.AlignForward((byte*)offsetPtr.ToPointer(), alignment) - (byte*)dataPtr.ToPointer());

            if (alignedOffset + totalBytes > dataCapacityBytes)
            {
                throw new InvalidOperationException("Persist allocator is out of memory.");
            }

            if (handleIndex >= maxHandle)
            {
                throw new InvalidOperationException("Persist allocator has reached maximum handle count.");
            }

            var handle = new HandleArray(Id, handleIndex, alignedOffset, capacity, handles[handleIndex].Generation);
            handles[handleIndex] = new Handle(Id, handleIndex, alignedOffset, handles[handleIndex].Generation);
            handleIndex++;
            offsetPtr = IntPtr.Add(dataPtr, alignedOffset + totalBytes);
            UpdatePeaks();
            return handle;
        }

        public void Deallocate(in Handle handle)
        {
            Logger.LogWarning("Persist allocator doesn't support deallocation.");
        }

        public void Deallocate(in HandleArray handle)
        {
            Logger.LogWarning("Persist allocator doesn't support deallocation.");
        }

        public T Get<T>(in Handle handle) where T : unmanaged
        {
            IntPtr handlePtr = IntPtr.Add(dataPtr, handle.ArrayIndex);
            return Marshal.PtrToStructure<T>(handlePtr);
        }

        public Span<T> GetArray<T>(in HandleArray handle) where T : unmanaged
        {
            IntPtr handlePtr = IntPtr.Add(dataPtr, handle.ArrayIndex);
            return new Span<T>(handlePtr.ToPointer(), handle.Capacity);
        }

        public void Set<T>(in Handle handle, T value) where T : unmanaged
        {
            IntPtr handlePtr = IntPtr.Add(dataPtr, handle.ArrayIndex);
            Marshal.StructureToPtr(value, handlePtr, false);
        }

        public void SetArray<T>(in HandleArray handle, in Span<T> values, int offset = 0) where T : unmanaged
        {
            IntPtr handlePtr = IntPtr.Add(dataPtr, handle.ArrayIndex + offset * sizeof(T));
            UnsafeUtility.MemCpy(handlePtr.ToPointer(), UnsafeUtility.AddressOf(ref values.GetPinnableReference()), (uint)(values.Length * sizeof(T)));
        }

        public void ResetPeaks()
        {
            peakCount = Count;
            peakHandleCount = HandleCount;
        }

        private void UpdatePeaks()
        {
            peakCount = Math.Max(peakCount, Count);
            peakHandleCount = Math.Max(peakHandleCount, HandleCount);
        }
    }
}
