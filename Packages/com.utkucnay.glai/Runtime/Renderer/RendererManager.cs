using Glai.Module;
using UnityEngine.Scripting;

namespace Glai.Renderer
{
    [Preserve, ModuleRegister(priority: 0)]
    public sealed class RendererManager : ModuleBase
    {
        public override void Initialize()
        {
        }

        public override void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            base.Dispose();
        }
    }
}
