using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Glai.Analytics;
using Glai.Core;
using Unity.Collections;

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
        
        public FixedString128Bytes Name { get; private set; }
        public int Count { get { return (int)((long)offsetPtr - (long)dataPtr) + (handleIndex * sizeof(Handle)); } }
        public int Capacity { get { return dataCapacityBytes + (maxHandle * sizeof(Handle)); } }

        public bool IsDisposed { get; private set; }


        public Stack(StackData data)
        {
            id = Guid.NewGuid();
            Name = data.name;

            handles = (Handle*)Marshal.AllocHGlobal(sizeof(Handle) * data.maxHandles);
            Unsafe.InitBlock(handles, 0, (uint)(sizeof(Handle) * data.maxHandles));
            handleIndex = 0;
            maxHandle = data.maxHandles;

            dataPtr = Marshal.AllocHGlobal(data.capacityBytes);
            Unsafe.InitBlock(dataPtr.ToPointer(), 0, (uint)data.capacityBytes);
            dataCapacityBytes = data.capacityBytes;
            offsetPtr = dataPtr;

            MemoryAnalytics.RegisterAllocator(this);
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

            MemoryAnalytics.UnregisterAllocator(this);
            Marshal.FreeHGlobal(dataPtr);
            Marshal.FreeHGlobal((IntPtr)handles);
            dataPtr = IntPtr.Zero;
            offsetPtr = IntPtr.Zero;
            handles = null;
            handleIndex = 0;
            maxHandle = 0;
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
            return handle;
        }

        public void Deallocate(in Handle handle)
        {
            if (!handle.IsValid(handles[handle.Index]))
            {
                throw new InvalidOperationException("Stack isn't valid.");
            }

            handles[handle.Index] = new Handle(id, handle.Index, handle.ArrayIndex, handles[handle.Index].Generation + 1);
            for (int handleIndex = handle.Index + 1; handleIndex < this.handleIndex; handleIndex++)
            {
                handles[handleIndex] = new Handle(id, handleIndex, handles[handleIndex].ArrayIndex, handles[handleIndex].Generation + 1);
            }
            handleIndex = handle.Index;
            offsetPtr = IntPtr.Add(dataPtr, handle.ArrayIndex);  
        }

        public void Deallocate(in HandleArray handle)
        {
            if (!handle.IsValid(handles[handle.Index]))
            {
                throw new InvalidOperationException("Stack isn't valid.");
            }

            handles[handle.Index] = new Handle(id, handle.Index, handle.ArrayIndex, handles[handle.Index].Generation + 1);
            for (int handleIndex = handle.Index + 1; handleIndex < this.handleIndex; handleIndex++)
            {
                handles[handleIndex] = new Handle(id, handleIndex, handles[handleIndex].ArrayIndex, handles[handleIndex].Generation + 1);
            }
            handleIndex = handle.Index;
            offsetPtr = IntPtr.Add(dataPtr, handle.ArrayIndex);
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
            Unsafe.CopyBlock(handlePtr.ToPointer(), Unsafe.AsPointer(ref values.GetPinnableReference()), (uint)(values.Length * sizeof(T)));
        }
    }
}
