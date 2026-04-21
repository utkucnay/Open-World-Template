using System;
using System.Collections.Generic;

namespace Glai.Core
{
    public static class TypeRegistry
    {
        private static int next = 0;
        private static readonly List<Type> typesById = new List<Type>();

        public static int Register<T>()
        {
            int id = next++;
            typesById.Add(typeof(T));
            return id;
        }

        public static Type GetType(int typeId)
        {
            return typeId >= 0 && typeId < typesById.Count ? typesById[typeId] : null;
        }

        public static string GetTypeName(int typeId)
        {
            Type type = GetType(typeId);
            return type != null ? type.Name : $"Type#{typeId}";
        }
    }

    public static class TypeId<T>
    {
        public static readonly int Id = TypeRegistry.Register<T>();
    }
}
