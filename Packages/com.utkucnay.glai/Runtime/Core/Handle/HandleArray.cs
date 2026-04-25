using System;

namespace Glai.Core
{
    public struct HandleArray
    {
        private Handle handle;
        public Guid Id { get { return handle.Id; } }
        public int Index { get { return handle.Index; } }
        public int ArrayIndex { get { return handle.ArrayIndex; } }
        public int Generation { get { return handle.Generation; } }
        public int Capacity { get; private set; }

        public HandleArray(Guid guid, int index, int arrayIndex, int capacity)
        {
            handle = new Handle(guid, index, arrayIndex, 0);
            Capacity = capacity;
        }  

        public HandleArray(Guid guid, int index, int arrayIndex, int capacity, int generation)
        {
            handle = new Handle(guid, index, arrayIndex, generation);
            Capacity = capacity;
        }    

        public bool IsValid(Handle validHandle) => handle.IsValid(validHandle);
    }
}