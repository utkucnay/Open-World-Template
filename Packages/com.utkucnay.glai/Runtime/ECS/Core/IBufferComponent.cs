using System;

// Defined in Glai.ECS.Core assembly so Chunk, BufferArray, and BufferTypeInfo
// (all in this assembly) can reference IBufferComponent without a circular dependency.
// The namespace is kept as Glai.ECS so user code only needs "using Glai.ECS;".
namespace Glai.ECS
{
    public interface IBufferComponent { }

    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class FixedBufferAttribute : Attribute
    {
        public int Capacity { get; }
        public FixedBufferAttribute(int capacity) => Capacity = capacity;
    }
}
