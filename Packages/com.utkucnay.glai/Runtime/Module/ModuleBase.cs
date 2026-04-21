using UnityEngine.Scripting;

namespace Glai.Module
{
    [Preserve]
    public abstract class ModuleBase : Glai.Core.Object
    {
        public abstract void Initialize();
    }
}