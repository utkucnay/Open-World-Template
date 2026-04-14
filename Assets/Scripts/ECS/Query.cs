using System.Collections.Generic;
using Glai.Core;
using Glai.ECS.Core;

namespace Glai.ECS
{
    public sealed class Query
    {
        private readonly List<int> requiredComponentTypeIds = new List<int>(8);
        private readonly HashSet<int> requiredComponentTypeIdSet = new HashSet<int>();

        public Query Has<T>() where T : unmanaged, IComponent
        {
            int componentTypeId = TypeId<T>.Id;
            if (requiredComponentTypeIdSet.Add(componentTypeId))
            {
                requiredComponentTypeIds.Add(componentTypeId);
            }

            return this;
        }

        internal bool Matches(in Archetype archetype)
        {
            if (requiredComponentTypeIds.Count > archetype.ComponentCount)
            {
                return false;
            }

            for (int i = 0; i < requiredComponentTypeIds.Count; i++)
            {
                if (!archetype.ContainsComponentType(requiredComponentTypeIds[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }   
}
