using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher
{
    internal class PatcherStrategyResolver : IPatcherStrategyResolver
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(PatcherStrategyResolver));

        public PatcherStrategyResolver()
        {
            DebugLogger.LogConstructor();
        }

        public IPatcherStrategy Resolve(PatcherContext context)
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
                    return new PatcherDiffStrategy(context);
                }
                DebugLogger.Log("Diff cost is higher than content cost - using content strategy.");
            }
            else
            {
                DebugLogger.Log("Application is not installed - using content strategy.");
            }

            return new PatcherContentStrategy(context);
        }

        private ulong GetContentCost(PatcherContext context)
        {
            DebugLogger.Log("Calculating content cost.");

            int latestVersionId = context.Data.RemoteData.MetaData.GetLatestVersionId();

            var contentSummary = context.Data.RemoteData.MetaData.GetContentSummary(latestVersionId);

            return (ulong)contentSummary.Size;
        }

        private ulong GetDiffCost(PatcherContext context)
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