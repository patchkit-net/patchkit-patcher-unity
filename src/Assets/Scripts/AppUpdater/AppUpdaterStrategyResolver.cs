using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    internal class AppUpdaterStrategyResolver : IAppUpdaterStrategyResolver
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

            if (context.Data.LocalData.IsInstalled())
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

            int latestVersionId = context.Data.RemoteData.MetaData.GetLatestVersionId();

            var contentSummary = context.Data.RemoteData.MetaData.GetContentSummary(latestVersionId);

            return (ulong)contentSummary.Size;
        }

        private ulong GetDiffCost(AppUpdaterContext context)
        {
            DebugLogger.Log("Calculating diff cost.");

            int latestVersionId = context.Data.RemoteData.MetaData.GetLatestVersionId();
            int currentLocalVersionId = context.Data.LocalData.GetInstalledVersion();

            ulong cost = 0;

            for (int i = currentLocalVersionId + 1; i <= latestVersionId; i++)
            {
                var diffSummary = context.Data.RemoteData.MetaData.GetDiffSummary(i);
                cost += (ulong)diffSummary.Size;
            }

            return cost;
        }
    }
}