using System;

namespace Glai.Module
{
    [Obsolete("Module registration now uses RuntimeModuleCatalog runtime bootstrap.")]
    [AttributeUsage(AttributeTargets.Class)]
    public class ModuleRegisterAttribute : Attribute
    {
    }
}
