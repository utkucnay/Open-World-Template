using System;

namespace Glai.ECS
{
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class QueryJobAttribute : Attribute
    {
        public QueryExecution Execution { get; }

        public QueryJobAttribute(QueryExecution execution)
        {
            Execution = execution;
        }
    }
}
