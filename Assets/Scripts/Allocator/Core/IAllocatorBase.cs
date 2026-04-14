using System;
using Unity.Collections;

namespace Glai.Allocator.Core
{
    public interface IAllocatorBase : IDisposable
    {
        FixedString128Bytes Name { get; }
        int Count { get; }
        int Capacity { get; }
    }
}