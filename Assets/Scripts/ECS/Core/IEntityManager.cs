namespace Glai.ECS.Core
{
    public interface IEntityManager
    {
        static IEntityManager Instance { get; protected set; }
    }
}