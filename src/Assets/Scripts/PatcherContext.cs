using PatchKit.Unity.Patcher.Progress;

namespace PatchKit.Unity.Patcher
{
    internal class PatcherContext
    {
        public readonly PatcherData Data;

        public readonly PatcherConfiguration Configuration;

        public readonly IProgressMonitor ProgressMonitor;

        public PatcherContext(PatcherData data, PatcherConfiguration configuration, IProgressMonitor progressMonitor)
        {
            Data = data;
            Configuration = configuration;
            ProgressMonitor = progressMonitor;
        }
    }
}