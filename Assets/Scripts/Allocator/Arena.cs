using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Glai.Core;
using Unity.Collections;

namespace Glai.Allocator
{
    public struct ArenaData
    {
        public FixedString128Bytes name;
        public int capacityBytes;
        public int maxHandles;
    }

    public unsafe class Arena : IAllocator
    {
        Guid id;
        
        IntPtr dataPtr;
        IntPtr offsetPtr;
        int capacityBytes;

        Handle* handles;
        int handleIndex;

        public FixedString128Bytes Name { get; private set; }
        public int Count { get { return (int)((long)offsetPtr - (long)dataPtr); } }
        public int Capacity { get { return (int)capacityBytes; } }

        public Arena(ArenaData data)
        {
            id = Guid.NewGuid();
            Name = data.name;

            handles = (Handle*)Marshal.AllocHGlobal(sizeof(Handle) * data.maxHandles);
            handleIndex = 0;

            dataPtr = Marshal.AllocHGlobal(data.capacityBytes);
            offsetPtr = dataPtr;
            capacityBytes = data.capacityBytes;
        }
        
        public void Dispose()
        {
            Marshal.FreeHGlobal(dataPtr);
            Marshal.FreeHGlobal((IntPtr)handles);
            dataPtr = IntPtr.Zero;
            offsetPtr = IntPtr.Zero;
            handles = null;
            capacityBytes = 0;
        }

        public Handle Allocate<T>() where T : unmanaged
        {
            int byteSize = sizeof(T);
            
            if ((long)offsetPtr + byteSize - (long)dataPtr > capacityBytes)
            {
                throw new InvalidOperationException("Arena is out of memory.");
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

            if ((long)offsetPtr + byteSize * capacity - (long)dataPtr > capacityBytes)
            {
                throw new InvalidOperationException("Arena is out of memory.");
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
                throw new InvalidOperationException("Arena isn't valid.");
            }

            handles[handle.Index] = new Handle(id, handle.Index, handle.ArrayIndex, handles[handle.Index].Generation + 1);      
        }

        public void Deallocate(in HandleArray handle)
        {
            if (!handle.IsValid(handles[handle.Index]))
            {
                throw new InvalidOperationException("Arena isn't valid.");
            }

            handles[handle.Index] = new Handle(id, handle.Index, handle.ArrayIndex, handles[handle.Index].Generation + 1);
        }

        public void Clear()
        {
            for (int i = 0; i < handleIndex; i++)
            {
                handles[i] = new Handle(id, handles[i].Index, handles[i].ArrayIndex, handles[i].Generation + 1);
            }

            handleIndex = 0;
            offsetPtr = dataPtr;
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
