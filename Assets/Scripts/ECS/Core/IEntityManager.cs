using Glai.Allocator;

namespace Glai.ECS.Core
{
    public interface IEntityManager
    {
        static IEntityManager Instance { get; protected set; }
        ECSMemoryState ECSMemoryState { get; }
    }
}