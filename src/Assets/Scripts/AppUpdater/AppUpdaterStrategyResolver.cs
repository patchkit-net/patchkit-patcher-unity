using System.Linq;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.Patcher.Cancellation;
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
            Checks.ArgumentNotNull(context, "context");

            DebugLogger.Log("Resolving best strategy for updating...");

            if (context.App.IsInstalled())
            {
                int installedVersionId = context.App.GetInstalledVersionId();
                int latestVersionId = context.App.GetLatestVersionId();

                if (installedVersionId == latestVersionId)
                {
                    DebugLogger.Log("Installed version is the same as the latest version. Using empty strategy.");

                    return new AppUpdaterEmptyStrategy();
                }

                if (installedVersionId < latestVersionId)
                {
                    DebugLogger.Log("Installed version is older than the latest version. Checking whether cost of updating with diff is lower than cost of updating with content...");

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

                            return new AppUpdaterContentStrategy(context);
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
                        DebugLogger.Log("Cost of updating with diff is lower than cost of updating with content. Using diff strategy.");

                        return new AppUpdaterDiffStrategy(context);
                    }

                    DebugLogger.Log("Cost of updating with content is lower than cost of updating with diff. Using content strategy.");
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

            return new AppUpdaterContentStrategy(context);
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