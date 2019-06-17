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
                case StrategyType.Repair:
                    return new AppUpdaterRepairStrategy(context, _status); 
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
                case StrategyType.Repair:
                    return StrategyType.Content;
                default:
                    return StrategyType.Content;
            }
        }

        public StrategyType Resolve([NotNull] AppUpdaterContext context, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            try
            {
                _logger.LogDebug("Resolving best strategy for updating...");

                bool isInstallationBroken = context.App.IsInstallationBroken();

                if (context.App.IsFullyInstalled() || isInstallationBroken)
                {
                    int installedVersionId = context.App.GetInstalledVersionId();
                    _logger.LogTrace("installedVersionId = " + installedVersionId);

                    int latestVersionId = context.App.GetLatestVersionId(true, cancellationToken);
                    _logger.LogTrace("latestVersionId = " + latestVersionId);

                    if (installedVersionId == latestVersionId)
                    {
                        _logger.LogDebug("Installed version is the same as the latest version. Using empty strategy.");

                        if (isInstallationBroken)
                        {
                            // Repair is always possible because we are fixing latest version
                            return StrategyType.Repair;
                        }

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
                        if (DoesVersionSupportDiffUpdates(context, installedVersionId, cancellationToken))
                        {
                            _logger.LogDebug("Installed version does not support diff updates. Using content strategy");

                            return StrategyType.Content;
                        }

                        _logger.LogDebug(
                            "Checking whether cost of updating with diff is lower than cost of updating with content...");

                        long contentSize = GetLatestVersionContentSize(context, cancellationToken);
                        _logger.LogTrace("contentSize = " + contentSize);

                        long sumDiffSize = GetLatestVersionDiffSizeSum(context, cancellationToken);
                        _logger.LogTrace("sumDiffSize = " + sumDiffSize);

                        if (sumDiffSize < contentSize)
                        {
                            _logger.LogDebug(
                                "Cost of updating with diff is lower than cost of updating with content.");

                            if (context.Configuration.CheckConsistencyBeforeDiffUpdate)
                            {
                                _logger.LogDebug("Checking consitency before allowing diff update...");

                                if (!IsVersionIntegral(contentSize, context, cancellationToken))
                                {
                                    if (IsRepairPossible(context, cancellationToken))
                                    {
                                        _logger.LogDebug("Installed version is broken. Using repair&diff strategy.");
                                        return StrategyType.RepairAndDiff;
                                    }
                                    else
                                    {
                                        _logger.LogDebug("Installed version is broken and repair is not possible. Using content strategy.");
                                        return StrategyType.Content;
                                    }   
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

        private bool IsRepairPossible(AppUpdaterContext context, CancellationToken cancellationToken)
        {
            if (!context.App.IsFullyInstalled() && !context.App.IsInstallationBroken())
            {
                return false;
            }

            int installedVersionId = context.App.GetInstalledVersionId();
            int lowestVersionWithContent = context.App.GetLowestVersionWithContentId(cancellationToken);

            return installedVersionId >= lowestVersionWithContent;
        }

        private bool DoesVersionSupportDiffUpdates(AppUpdaterContext context, int versionId, CancellationToken cancellationToken)
        {
            int lowestVersionWithDiffId = context.App.GetLowestVersionWithDiffId(cancellationToken);
            _logger.LogTrace("lowestVersionWithDiffId = " + lowestVersionWithDiffId);

            return versionId + 1 < lowestVersionWithDiffId;
        }

        private bool IsVersionIntegral(long contentSize, AppUpdaterContext context, CancellationToken cancellationToken)
        {
            var commandFactory = new AppUpdaterCommandFactory();
            int installedVersionId = context.App.GetInstalledVersionId();
            _logger.LogTrace("installedVersionId = " + installedVersionId);
            _logger.LogTrace("context.Configuration.HashSizeThreshold = " + context.Configuration.HashSizeThreshold);

            bool isCheckingHash = contentSize < context.Configuration.HashSizeThreshold;
            _logger.LogTrace("isCheckingHash = " + isCheckingHash);

            var checkVersionIntegrity = commandFactory.CreateCheckVersionIntegrityCommand(
                installedVersionId, context, isCheckingHash, true, cancellationToken);

            checkVersionIntegrity.Prepare(_status, cancellationToken);
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

        private static long GetLatestVersionContentSize(AppUpdaterContext context, CancellationToken cancellationToken)
        {
            int latestVersionId = context.App.GetLatestVersionId(true, cancellationToken);
            var contentSummary = context.App.RemoteMetaData.GetContentSummary(latestVersionId, cancellationToken);
            return contentSummary.Size;
        }

        private static long GetLatestVersionDiffSizeSum(AppUpdaterContext context, CancellationToken cancellationToken)
        {
            int latestVersionId = context.App.GetLatestVersionId(true, cancellationToken);
            int currentLocalVersionId = context.App.GetInstalledVersionId();

            long cost = 0;

            for (int i = currentLocalVersionId + 1; i <= latestVersionId; i++)
            {
                var diffSummary = context.App.RemoteMetaData.GetDiffSummary(i, cancellationToken);
                cost += diffSummary.Size;
            }

            return cost;
        }
    }
}