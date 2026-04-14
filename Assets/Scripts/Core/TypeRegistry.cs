namespace Glai.Core
{
    public static class TypeRegistry
    {
        private static int next = 0;

        public static int Register<T>()
        {
            return next++;
        }
    }

    public static class TypeId<T>
    {
        public static readonly int Id = TypeRegistry.Register<T>();
    }
}