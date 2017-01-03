using PatchKit.Unity.Patcher.Progress;

namespace PatchKit.Unity.Patcher
{
    internal class PatcherContext
    {
        public readonly PatcherData Data;

        public readonly PatcherConfiguration Configuration;

        public readonly IStatusMonitor StatusMonitor;

        public PatcherContext(PatcherData data, PatcherConfiguration configuration, IStatusMonitor statusMonitor)
        {
            Data = data;
            Configuration = configuration;
            StatusMonitor = statusMonitor;
        }
    }
}