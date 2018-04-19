using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Api.Models.Main;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdater
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdater));

        public readonly AppUpdaterContext Context;

        private readonly UpdaterStatus _status = new UpdaterStatus();

        private IAppUpdaterStrategyResolver _strategyResolver;

        private IAppUpdaterStrategy _strategy;

        private bool _updateHasBeenCalled;

        public IReadOnlyUpdaterStatus Status
        {
            get { return _status; }
        }

        public AppUpdater(AppUpdaterContext context)
        {
            Checks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _strategyResolver = new AppUpdaterStrategyResolver(_status);
            Context = context;
        }

        private void PreUpdate(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Pre update integrity check");

            var commandFactory = new AppUpdaterCommandFactory();

            int installedVersionId = Context.App.GetInstalledVersionId();
            int latestVersionId = Context.App.GetLatestVersionId();

            AppContentSummary installedVersionContentSummary 
                = Context.App.RemoteMetaData.GetContentSummary(installedVersionId);
                
            AppContentSummary latestVersionContentSummary 
                = Context.App.RemoteMetaData.GetContentSummary(latestVersionId);
            
            bool isNewVersionAvailable = installedVersionId < latestVersionId;

            long contentSize = isNewVersionAvailable
                ? latestVersionContentSummary.Size
                : installedVersionContentSummary.Size;
            
            ICheckVersionIntegrityCommand checkIntegrity = commandFactory
                .CreateCheckVersionIntegrityCommand(
                    versionId: installedVersionId, 
                    context: Context, 
                    isCheckingHash: false, 
                    isCheckingSize: true);
            
            checkIntegrity.Prepare(_status);
            checkIntegrity.Execute(cancellationToken);
            
            int missingFilesCount = checkIntegrity.Results.Files
                .Select(f => f.Status == FileIntegrityStatus.MissingData)
                .Count();
            
            int invalidSizeFilesCount = checkIntegrity.Results.Files
                .Select(f => f.Status == FileIntegrityStatus.InvalidSize)
                .Count();

            if (missingFilesCount + invalidSizeFilesCount == 0)
            {
                DebugLogger.Log("No missing or invalid size files.");
                return;
            }
            
            var repairStrategy = new AppUpdaterRepairAndDiffStrategy(Context, _status, performDiff: false);

            double repairCost = (missingFilesCount + invalidSizeFilesCount) * 2;
            if (isNewVersionAvailable)
            {
                repairCost *= latestVersionContentSummary.Chunks.Size;
            }
            else
            {
                repairCost *= installedVersionContentSummary.Chunks.Size;
            }

            if (repairCost < contentSize)
            {
                DebugLogger.Log(string.Format("Repair cost {0} is smaller than content cost {1}, repairing...", repairCost, contentSize));
                repairStrategy.Update(cancellationToken);
            }
            else
            {
                DebugLogger.Log("Content cost is smaller than repair.");
            }
        }

        public void Update(CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _updateHasBeenCalled, "Update");

            if (Context.App.IsInstallationBroken() || Context.App.IsFullyInstalled())
            {
                PreUpdate(cancellationToken);
            }

            DebugLogger.Log("Updating.");

            StrategyType type = _strategyResolver.Resolve(Context);
            _strategy = _strategyResolver.Create(type, Context);

            try
            {
                _strategy.Update(cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException
                    || ex is UnauthorizedAccessException
                    || ex is NotEnoughtDiskSpaceException
                    || ex is ThreadInterruptedException
                    || ex is ThreadAbortException)
                {
                    DebugLogger.LogWarning("Strategy caused exception, to be handled further");
                    throw;
                }
                else
                {
                    DebugLogger.LogWarningFormat("Strategy caused exception, being handled by fallback: {0}, Trace: {1}", ex, ex.StackTrace);

                    if (!TryHandleFallback(cancellationToken))
                    {
                        throw;
                    }
                }
            }
        }

        private bool TryHandleFallback(CancellationToken cancellationToken)
        {
            var fallbackType = _strategyResolver.GetFallbackStrategy(_strategy.GetStrategyType());

            if (fallbackType == StrategyType.None)
            {
                return false;
            }

            _strategy = _strategyResolver.Create(fallbackType, Context);

            _strategy.Update(cancellationToken);

            return true;
        }
    }
}