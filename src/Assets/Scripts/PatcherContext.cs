using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher
{
    internal class PatcherContext
    {
        public readonly PatcherData Data;

        public readonly PatcherConfiguration Configuration;

        public readonly IStatusMonitor StatusMonitor;

        public PatcherContext(PatcherData data, PatcherConfiguration configuration, IStatusMonitor statusMonitor)
        {
            AssertChecks.ArgumentNotNull(data, "data");
            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");

            Data = data;
            Configuration = configuration;
            StatusMonitor = statusMonitor;
        }
    }
}