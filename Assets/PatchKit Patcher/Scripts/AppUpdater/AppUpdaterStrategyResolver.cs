using System;
using System.Linq;
using JetBrains.Annotations;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterStrategyResolver: IAppUpdaterStrategyResolver
    {
        private readonly ILogger _logger;

        private readonly UpdaterStatus _status;

        public AppUpdaterStrategyResolver(UpdaterStatus status)
        {
            _logger = PatcherLogManager.DefaultLogger;
            _status = status;
        }

        public IAppUpdaterStrategy Create(StrategyType type, AppUpdaterContext context)
        {
            switch (type)
            {
                case StrategyType.Empty:
                    return new AppUpdaterEmptyStrategy();
                case StrategyType.Content:
                    return new AppUpdaterContentStrategy(context, _status);
                case StrategyType.Diff:
                    return new AppUpdaterDiffStrategy(context, _status);
                case StrategyType.RepairAndDiff:
                    return new AppUpdaterRepairAndDiffStrategy(context, _status);
                default:
                    return new AppUpdaterContentStrategy(context, _status);
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
                case StrategyType.RepairAndDiff:
                    return StrategyType.Content;
                default:
                    return StrategyType.Content;
            }
        }

        public StrategyType Resolve([NotNull] AppUpdaterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            try
            {
                _logger.LogDebug("Resolving best strategy for updating...");

                if (context.App.IsFullyInstalled())
                {
                    int installedVersionId = context.App.GetInstalledVersionId();
                    _logger.LogTrace("installedVersionId = " + installedVersionId);

                    int latestVersionId = context.App.GetLatestVersionId();
                    _logger.LogTrace("latestVersionId = " + latestVersionId);

                    if (installedVersionId == latestVersionId)
                    {
                        _logger.LogDebug("Installed version is the same as the latest version. Using empty strategy.");

                        return StrategyType.Empty;
                    }

                    if (installedVersionId < latestVersionId)
                    {
                        _logger.LogDebug("Installed verison is older than latest version...");

#if PATCHKIT_DONT_USE_DIFF_UPDATES
                    _logger.LogDebug(
                        "Installed version is older than the latest version. " +
                        "Using content strategy due to define PATCHKIT_DONT_USE_DIFF_UPDATES");

                    return StrategyType.Content;
#else
                        if (DoesVersionSupportDiffUpdates(context, installedVersionId))
                        {
                            _logger.LogDebug("Installed version does not support diff updates. Using content strategy");

                            return StrategyType.Content;
                        }

                        _logger.LogDebug(
                            "Checking whether cost of updating with diff is lower than cost of updating with content...");

                        long contentSize = GetLatestVersionContentSize(context);
                        _logger.LogTrace("contentSize = " + contentSize);

                        long sumDiffSize = GetLatestVersionDiffSizeSum(context);
                        _logger.LogTrace("sumDiffSize = " + sumDiffSize);

                        if (sumDiffSize < contentSize)
                        {
                            _logger.LogDebug(
                                "Cost of updating with diff is lower than cost of updating with content.");

                            if (context.Configuration.CheckConsistencyBeforeDiffUpdate)
                            {
                                _logger.LogDebug("Checking consitency before allowing diff update...");

                                if (!IsVersionIntegral(contentSize, context))
                                {
                                    _logger.LogDebug("Installed version is broken. Using repair&diff strategy.");
                                    return StrategyType.RepairAndDiff;
                                }

                                _logger.LogDebug("Installed verison is ready for diff updating.");
                            }

                            _logger.LogDebug("Using diff strategy.");

                            return StrategyType.Diff;
                        }

                        _logger.LogDebug(
                            "Cost of updating with content is lower than cost of updating with diff. Using content strategy.");
#endif
                    }
                    else
                    {
                        _logger.LogDebug("Installed version is newer than the latest version. Using content strategy.");
                    }
                }
                else
                {
                    _logger.LogDebug("Application is not installed. Using content strategy.");
                }

                return StrategyType.Content;
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to resolve best strategy for updating.", e);
                throw;
            }
        }

        private bool DoesVersionSupportDiffUpdates(AppUpdaterContext context, int versionId)
        {
            int lowestVersionWithDiffId = GetLowestVersionWithDiffId(context);
            _logger.LogTrace("lowestVersionWithDiffId = " + lowestVersionWithDiffId);

            return versionId + 1 < lowestVersionWithDiffId;
        }

        private bool IsVersionIntegral(long contentSize, AppUpdaterContext context)
        {
            var commandFactory = new AppUpdaterCommandFactory();
            int installedVersionId = context.App.GetInstalledVersionId();
            _logger.LogTrace("installedVersionId = " + installedVersionId);
            _logger.LogTrace("context.Configuration.HashSizeThreshold = " + context.Configuration.HashSizeThreshold);

            bool isCheckingHash = contentSize < context.Configuration.HashSizeThreshold;
            _logger.LogTrace("isCheckingHash = " + isCheckingHash);

            var checkVersionIntegrity = commandFactory.CreateCheckVersionIntegrityCommand(
                installedVersionId, context, isCheckingHash);

            checkVersionIntegrity.Prepare(_status);
            checkVersionIntegrity.Execute(CancellationToken.Empty);

            bool isValid = checkVersionIntegrity.Results.Files.All(
                fileIntegrity => fileIntegrity.Status == FileIntegrityStatus.Ok);

            if (!isValid)
            {
                foreach (var fileIntegrity in checkVersionIntegrity.Results.Files)
                {
                    if (fileIntegrity.Status != FileIntegrityStatus.Ok)
                    {
                        string logMessage = string.Format("File {0} is not consistent - {1}", 
                            fileIntegrity.FileName, fileIntegrity.Status);

                        if (!string.IsNullOrEmpty(fileIntegrity.Message))
                        {
                            logMessage += " - " + fileIntegrity.Message;
                        }

                        _logger.LogDebug(logMessage);
                    }
                }
            }

            return isValid;
        }

        private static int GetLowestVersionWithDiffId(AppUpdaterContext context)
        {
            var appInfo = context.App.RemoteMetaData.GetAppInfo();
            return appInfo.LowestVersionWithDiff;
        }

        private static long GetLatestVersionContentSize(AppUpdaterContext context)
        {
            int latestVersionId = context.App.GetLatestVersionId();
            var contentSummary = context.App.RemoteMetaData.GetContentSummary(latestVersionId);
            return contentSummary.Size;
        }

        private static long GetLatestVersionDiffSizeSum(AppUpdaterContext context)
        {
            int latestVersionId = context.App.GetLatestVersionId();
            int currentLocalVersionId = context.App.GetInstalledVersionId();

            long cost = 0;

            for (int i = currentLocalVersionId + 1; i <= latestVersionId; i++)
            {
                var diffSummary = context.App.RemoteMetaData.GetDiffSummary(i);
                cost += diffSummary.Size;
            }

            return cost;
        }
    }
}