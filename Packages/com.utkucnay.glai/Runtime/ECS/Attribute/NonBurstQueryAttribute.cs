using System;

namespace Glai.ECS
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class NonBurstQueryAttribute : Attribute
    {
    }
}
