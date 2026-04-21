using System;
using System.Runtime.InteropServices;
using Glai.Analytics;
using Glai.Collections;
using Glai.Core;
using Unity.Collections.LowLevel.Unsafe;

namespace Glai.Allocator
{
    public struct StackData
    {
        public FixedString128Bytes name;
        public int capacityBytes;
        public int maxHandles;
    }

    public unsafe class Stack : IAllocator
    {
        Guid id;

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
        public int Capacity { get { return dataCapacityBytes + (maxHandle * sizeof(Handle)); } }

        public int HandleCount { get { return handleIndex; } }
        public int PeakHandleCount { get { return peakHandleCount; } }
        public int HandleCapacity { get { return maxHandle; } }

        public bool IsDisposed { get; private set; }


        public Stack(StackData data)
        {
            id = Guid.NewGuid();
            Name = data.name;

            handles = (Handle*)Marshal.AllocHGlobal(sizeof(Handle) * data.maxHandles);
            UnsafeUtility.MemClear(handles, (uint)(sizeof(Handle) * data.maxHandles));
            handleIndex = 0;
            maxHandle = data.maxHandles;
            peakHandleCount = 0;

            dataPtr = Marshal.AllocHGlobal(data.capacityBytes);
            UnsafeUtility.MemClear(dataPtr.ToPointer(), (uint)data.capacityBytes);
            dataCapacityBytes = data.capacityBytes;
            offsetPtr = dataPtr;
            peakCount = 0;

            AnalyticsManager.RegisterAllocator(this);
        }

        ~Stack()
        {
            if (!IsDisposed)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (IsDisposed) return;

            AnalyticsManager.UnregisterAllocator(this);
            Marshal.FreeHGlobal(dataPtr);
            Marshal.FreeHGlobal((IntPtr)handles);
            dataPtr = IntPtr.Zero;
            offsetPtr = IntPtr.Zero;
            handles = null;
            handleIndex = 0;
            maxHandle = 0;
            peakCount = 0;
            peakHandleCount = 0;
            IsDisposed = true;
        }   

        public Handle Allocate<T>() where T : unmanaged
        {
            int byteSize = sizeof(T);

            if ((long)offsetPtr + byteSize - (long)dataPtr > dataCapacityBytes)
            {
                throw new InvalidOperationException("Stack allocator is out of memory.");
            }

            if (handleIndex >= maxHandle)
            {
                throw new InvalidOperationException("Stack allocator has reached maximum handle count.");
            }

            var handle = new Handle(id, handleIndex, (int)((long)offsetPtr - (long)dataPtr), handles[handleIndex].Generation);
            handles[handleIndex] = handle;
            handleIndex++;
            offsetPtr = IntPtr.Add(offsetPtr, byteSize);
            UpdatePeaks();
            return handle;        
        }

        public HandleArray AllocateArray<T>(int capacity) where T : unmanaged
        {
            int byteSize = sizeof(T);
            
            if ((long)offsetPtr + byteSize * capacity - (long)dataPtr > dataCapacityBytes)
            {
                throw new InvalidOperationException("Stack allocator is out of memory.");
            }
            
            if (handleIndex >= maxHandle)
            {
                throw new InvalidOperationException("Stack allocator has reached maximum handle count.");
            }

            var handle = new HandleArray(id, handleIndex, (int)((long)offsetPtr - (long)dataPtr), capacity, handles[handleIndex].Generation);
            handles[handleIndex] = new Handle(id, handleIndex, (int)((long)offsetPtr - (long)dataPtr), handles[handleIndex].Generation);
            handleIndex++;
            offsetPtr = IntPtr.Add(offsetPtr, byteSize * capacity);
            UpdatePeaks();
            return handle;
        }

        public void Deallocate(in Handle handle)
        {
            if (!handle.IsValid(handles[handle.Index]))
            {
                Logger.LogWarning("Stack isn't valid, Deallocated before");
                return;
            }

            handles[handle.Index] = new Handle(id, handle.Index, handle.ArrayIndex, handles[handle.Index].Generation + 1);
            for (int handleIndex = handle.Index + 1; handleIndex < this.handleIndex; handleIndex++)
            {
                handles[handleIndex] = new Handle(id, handleIndex, handles[handleIndex].ArrayIndex, handles[handleIndex].Generation + 1);
            }
            handleIndex = handle.Index;
            offsetPtr = IntPtr.Add(dataPtr, handle.ArrayIndex);  
            UnsafeUtility.MemClear(offsetPtr.ToPointer(), (uint)(dataCapacityBytes - ((long)offsetPtr - (long)dataPtr)));
        }

        public void Deallocate(in HandleArray handle)
        {
            if (!handle.IsValid(handles[handle.Index]))
            {
                Logger.LogWarning("Stack isn't valid, Deallocated before");
                return;
            }

            handles[handle.Index] = new Handle(id, handle.Index, handle.ArrayIndex, handles[handle.Index].Generation + 1);
            for (int handleIndex = handle.Index + 1; handleIndex < this.handleIndex; handleIndex++)
            {
                handles[handleIndex] = new Handle(id, handleIndex, handles[handleIndex].ArrayIndex, handles[handleIndex].Generation + 1);
            }
            handleIndex = handle.Index;
            offsetPtr = IntPtr.Add(dataPtr, handle.ArrayIndex);
            UnsafeUtility.MemClear(offsetPtr.ToPointer(), (uint)(dataCapacityBytes - ((long)offsetPtr - (long)dataPtr)));
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
