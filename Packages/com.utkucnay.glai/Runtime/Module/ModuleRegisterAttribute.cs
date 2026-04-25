using System;

namespace Glai.Module
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ModuleRegisterAttribute : Attribute
    {
        public int Priority { get; }

        public ModuleRegisterAttribute(int priority = 0)
        {
            Priority = priority;
        }
    }
}
