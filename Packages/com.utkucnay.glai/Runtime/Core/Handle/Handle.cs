using System;
using UnityEngine;

namespace Glai.Core
{
    public struct Handle : IEquatable<Handle>
    {
        public Guid Id { get; private set; }
        public int Index { get; private set; }
        public int ArrayIndex { get; private set; }
        public int Generation { get; private set; }

        public Handle(Guid guid, int index, int arrayIndex)
        {
            Id = guid;
            Index = index;
            ArrayIndex = arrayIndex;
            Generation = 0;
        }  

        public Handle(Guid guid, int index, int arrayIndex, int generation)
        {
            Id = guid;
            Index = index;
            ArrayIndex = arrayIndex;
            Generation = generation;
        }    

        public bool IsValid(Handle validHandle)
        {            
            return Id == validHandle.Id && Index != -1 && Generation == validHandle.Generation;
        }

        public bool Equals(Handle other)
        {
            return Id == other.Id && Index == other.Index && ArrayIndex == other.ArrayIndex && Generation == other.Generation;
        }
    }
}

