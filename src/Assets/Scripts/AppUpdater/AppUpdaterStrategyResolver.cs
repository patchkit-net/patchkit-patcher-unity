using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterStrategyResolver : IAppUpdaterStrategyResolver
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdaterStrategyResolver));

        public AppUpdaterStrategyResolver()
        {
            DebugLogger.LogConstructor();
        }

        public IAppUpdaterStrategy Resolve(AppUpdaterContext context)
        {
            AssertChecks.ArgumentNotNull(context, "context");

            DebugLogger.Log("Resolving best strategy for patcher.");

            if (context.App.IsInstalled())
            {
                var diffCost = GetDiffCost(context);
                DebugLogger.LogVariable(diffCost, "diffCost");

                var contentCost = GetContentCost(context);
                DebugLogger.LogVariable(contentCost, "contentCost");

                if (diffCost < contentCost)
                {
                    DebugLogger.Log("Diff cost is lower than content cost - using diff strategy.");
                    return new AppUpdaterDiffStrategy(context);
                }
                DebugLogger.Log("Diff cost is higher than content cost - using content strategy.");
            }
            else
            {
                DebugLogger.Log("Application is not installed - using content strategy.");
            }

            return new AppUpdaterContentStrategy(context);
        }

        private ulong GetContentCost(AppUpdaterContext context)
        {
            DebugLogger.Log("Calculating content cost.");

            int latestVersionId = context.App.RemoteMetaData.GetLatestVersionId();

            var contentSummary = context.App.RemoteMetaData.GetContentSummary(latestVersionId);

            return (ulong)contentSummary.Size;
        }

        private ulong GetDiffCost(AppUpdaterContext context)
        {
            DebugLogger.Log("Calculating diff cost.");

            int latestVersionId = context.App.RemoteMetaData.GetLatestVersionId();
            int currentLocalVersionId = context.App.GetInstalledVersionId();

            ulong cost = 0;

            for (int i = currentLocalVersionId + 1; i <= latestVersionId; i++)
            {
                var diffSummary = context.App.RemoteMetaData.GetDiffSummary(i);
                cost += (ulong)diffSummary.Size;
            }

            return cost;
        }
    }
}