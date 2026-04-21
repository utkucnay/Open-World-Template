using System;
using Glai.Core;
using Glai.Tween.Core;

namespace Glai.Tween
{
    internal enum TweenType
    {
        None,
        Position,
        Rotation,
        Scale
    }

    public struct TweenHandle : IEquatable<TweenHandle>
    {
        private Handle handle;

        public Guid Id => handle.Id;
        public int Index => handle.Index;
        public int ArrayIndex => handle.ArrayIndex;
        public int Generation => handle.Generation;

        internal bool IsActive { get; private set; } 

        internal TweenType Type { get; private set; }

        internal bool AutoDispatch { get; private set; }

        internal TweenHandle(Guid id, int index, int arrayIndex, int generation, TweenType type, bool isActive = true, bool autoDispatch = true)
        {
            this.handle = new Handle(id, index, arrayIndex, generation);
            this.Type = type;
            this.IsActive = isActive;
            this.AutoDispatch = autoDispatch;
        }

        internal bool IsValid(TweenHandle validHandle) => handle.IsValid(validHandle.handle) && Type == validHandle.Type;

        public bool Equals(TweenHandle other)
        {
            return handle.Equals(other.handle) && Type == other.Type;
        }
    }
}