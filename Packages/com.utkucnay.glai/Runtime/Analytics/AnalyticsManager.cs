
using Glai.Analytics.Memory;
using Glai.Module;
using UnityEngine.Scripting;

namespace Glai.Analytics
{
    [Preserve, ModuleRegister(priority: -200)]
    public class AnalyticsManager : ModuleBase
    {
        MemoryAnalytics memoryAnalytics;

        public MemoryAnalytics MemoryAnalytics => memoryAnalytics;

        public override void Initialize()
        {
            memoryAnalytics = new MemoryAnalytics();
        }
    }
}
