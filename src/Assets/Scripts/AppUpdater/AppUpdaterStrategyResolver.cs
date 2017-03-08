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
            AssertChecks.ArgumentNotNull(context, "context");

            DebugLogger.Log("Resolving best strategy for patcher.");

            if (context.App.IsInstalled())
            {
                DebugLogger.Log("Application is installed.");

                int installedVersionId = context.App.GetInstalledVersionId();
                int latestVersionId = context.App.GetLatestVersionId();

                if (installedVersionId == latestVersionId)
                {
                    DebugLogger.Log("Installed version is the same as the latest version.");

                    return new AppUpdaterEmptyStrategy();
                }

                if (installedVersionId < latestVersionId)
                {
                    DebugLogger.Log("Installed version is older than the latest version.");

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
                    DebugLogger.Log("Installed version is newer than the latest version - using content strategy.");
                }
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

            int latestVersionId = context.App.GetLatestVersionId();

            var contentSummary = context.App.RemoteMetaData.GetContentSummary(latestVersionId);

            return (ulong)contentSummary.Size;
        }

        private ulong GetDiffCost(AppUpdaterContext context)
        {
            DebugLogger.Log("Calculating diff cost.");

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