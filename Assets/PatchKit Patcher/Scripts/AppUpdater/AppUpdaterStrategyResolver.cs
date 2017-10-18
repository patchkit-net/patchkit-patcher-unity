using System;
using System.Linq;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterStrategyResolver: IAppUpdaterStrategyResolver
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdaterStrategyResolver));

        public AppUpdaterStrategyResolver()
        {
            DebugLogger.LogConstructor();
        }

        public IAppUpdaterStrategy Create(StrategyType type, AppUpdaterContext context)
        {
            switch (type)
            {
                case StrategyType.Empty:
                    return new AppUpdaterEmptyStrategy();
                case StrategyType.Content:
                    return new AppUpdaterContentStrategy(context);
                case StrategyType.Diff:
                    return new AppUpdaterDiffStrategy(context);
                default:
                    return new AppUpdaterContentStrategy(context);
            }
        }

        public StrategyType GetFallbackStrategy(StrategyType strategyType)
        {
            switch (strategyType)
            {
                case StrategyType.Empty:
                    return StrategyType.Empty;
                case StrategyType.Content:
                    return StrategyType.None;
                case StrategyType.Diff:
                    return StrategyType.Content;
                default:
                    return StrategyType.Content;
            }
        }

        public StrategyType Resolve(AppUpdaterContext context)
        {
            Checks.ArgumentNotNull(context, "context");

            DebugLogger.Log("Resolving best strategy for updating...");

            if (context.App.IsInstalled())
            {
                int installedVersionId = context.App.GetInstalledVersionId();
                int latestVersionId = context.App.GetLatestVersionId();

                if (installedVersionId == latestVersionId)
                {
                    DebugLogger.Log("Installed version is the same as the latest version. Using empty strategy.");

                    return StrategyType.Empty;
                }

                if (installedVersionId < latestVersionId)
                {
#if PATCHKIT_DONT_USE_DIFF_UPDATES
                    DebugLogger.Log(
                        "Installed version is older than the latest version. " +
                        "Using content strategy due to define PATCHKIT_DONT_USE_DIFF_UPDATES");
                    
                    return StrategyType.Content;
#else
                    DebugLogger.Log(
                        "Installed version is older than the latest version. Checking whether cost of updating with diff is lower than cost of updating with content...");

                    if (context.Configuration.CheckConsistencyBeforeDiffUpdate)
                    {
                        DebugLogger.Log("Checking consitency before allowing diff update...");

                        var commandFactory = new AppUpdaterCommandFactory();

                        var checkVersionIntegrity = commandFactory.CreateCheckVersionIntegrityCommand(
                            installedVersionId, context);

                        checkVersionIntegrity.Prepare(context.StatusMonitor);
                        checkVersionIntegrity.Execute(CancellationToken.Empty);

                        if (checkVersionIntegrity.Results.Files.All(
                            fileIntegrity => fileIntegrity.Status == FileIntegrityStatus.Ok))
                        {
                            DebugLogger.Log("Version is consistent. Diff update is allowed.");
                        }
                        else
                        {
                            foreach (var fileIntegrity in checkVersionIntegrity.Results.Files)
                            {
                                if (fileIntegrity.Status != FileIntegrityStatus.Ok)
                                {
                                    DebugLogger.Log(string.Format("File {0} is not consistent - {1}",
                                        fileIntegrity.FileName, fileIntegrity.Status));
                                }
                            }

                            DebugLogger.Log(
                                "Version is not consistent. Diff update is forbidden - using content strategy.");

                            return StrategyType.Content;
                        }
                    }

                    var diffCost = GetDiffCost(context);
                    DebugLogger.LogVariable(diffCost, "diffCost");

                    DebugLogger.Log(string.Format("Cost of updating with diff equals {0}.", diffCost));

                    var contentCost = GetContentCost(context);
                    DebugLogger.LogVariable(contentCost, "contentCost");

                    DebugLogger.Log(string.Format("Cost of updating with content equals {0}.", contentCost));

                    if (diffCost < contentCost)
                    {
                        DebugLogger.Log(
                            "Cost of updating with diff is lower than cost of updating with content. Using diff strategy.");

                        return StrategyType.Diff;
                    }

                    DebugLogger.Log(
                        "Cost of updating with content is lower than cost of updating with diff. Using content strategy.");
#endif
                }
                else
                {
                    DebugLogger.Log("Installed version is newer than the latest version. Using content strategy.");
                }
            }
            else
            {
                DebugLogger.Log("Application is not installed. Using content strategy.");
            }

            return StrategyType.Content;
        }

        private ulong GetContentCost(AppUpdaterContext context)
        {
            int latestVersionId = context.App.GetLatestVersionId();

            var contentSummary = context.App.RemoteMetaData.GetContentSummary(latestVersionId);

            return (ulong)contentSummary.Size;
        }

        private ulong GetDiffCost(AppUpdaterContext context)
        {
            int latestVersionId = context.App.GetLatestVersionId();

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