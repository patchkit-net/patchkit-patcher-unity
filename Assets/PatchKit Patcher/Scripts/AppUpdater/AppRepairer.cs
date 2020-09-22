using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Collections.Generic;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Api.Models.Main;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppRepairer
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppRepairer));

        public readonly AppUpdaterContext Context;

        // set to true if you wish to check file hashes
        public bool CheckHashes = false;

        // how many times process will repeat until it ultimately fails
        public int RepeatCount = 3;

        private UpdaterStatus _status;

        private AppUpdaterStrategyResolver _strategyResolver;

        private AppUpdaterCommandFactory _commandFactory;

        private int _lowestVersionWithContentId;

        private const double IncreaseRepairCost = 1.5d;


        public AppRepairer(AppUpdaterContext context, UpdaterStatus status)
        {
            DebugLogger.LogConstructor();

            Checks.ArgumentNotNull(context, "context");

            Context = context;
            _status = status;

            _strategyResolver = new AppUpdaterStrategyResolver(_status);
            _commandFactory = new AppUpdaterCommandFactory();
        }

        // returns true if data is valid (was valid from the start or successfull repair was performed)
        public bool Perform(PatchKit.Unity.Patcher.Cancellation.CancellationToken cancellationToken)
        {
            _lowestVersionWithContentId = Context.App.GetLowestVersionWithContentId(cancellationToken);

            for(int attempt = 1; attempt <= RepeatCount; ++attempt)
            {
                DebugLogger.Log("Running integrity check, attempt " + attempt + " of " + RepeatCount);

                if (PerformInternal(cancellationToken))
                {
                    return true;
                }
            }

            // retry count reached, let's check for the last time if data is ok, but without repairing
            int installedVersionId = Context.App.GetInstalledVersionId();
            VersionIntegrity results = CheckIntegrity(cancellationToken, installedVersionId);
            var filesNeedFixing = FilesNeedFixing(results);

            if (filesNeedFixing.Count() == 0)
            {
                DebugLogger.Log("No missing or invalid size files.");
                return true;
            }
            

            DebugLogger.LogError("Still have corrupted files after all fixing attempts");
            return false;
        }

        // returns true if there was no integrity errors, false if there was and repair was performed
        private bool PerformInternal(PatchKit.Unity.Patcher.Cancellation.CancellationToken cancellationToken)
        {
            int installedVersionId = Context.App.GetInstalledVersionId();

            VersionIntegrity results = CheckIntegrity(cancellationToken, installedVersionId);
            var filesNeedFixing = FilesNeedFixing(results);

            if (filesNeedFixing.Count() == 0)
            {
                DebugLogger.Log("No missing or invalid size files.");
                return true;
            }

            // need to collect some data about the application to calculate the repair cost and make decisions
            
            int latestVersionId = Context.App.GetLatestVersionId(true, cancellationToken);

            AppContentSummary installedVersionContentSummary
                = Context.App.RemoteMetaData.GetContentSummary(installedVersionId, cancellationToken);

            AppContentSummary latestVersionContentSummary
                = Context.App.RemoteMetaData.GetContentSummary(latestVersionId, cancellationToken);

            bool isNewVersionAvailable = installedVersionId < latestVersionId;

            long contentSize = isNewVersionAvailable
                ? latestVersionContentSummary.Files.Sum(f => f.Size)
                : installedVersionContentSummary.Files.Sum(f => f.Size);

            double repairCost = CalculateRepairCost(installedVersionContentSummary, filesNeedFixing);
            
            // increasing repair costs that reinstallation will be done for 1/3 of the content size
            repairCost *= IncreaseRepairCost;


            if (_lowestVersionWithContentId > installedVersionId)
            {
                DebugLogger.Log(
                    "Repair is impossible because lowest version with content id is "
                    + _lowestVersionWithContentId +
                    " and currently installed version id is "
                    + installedVersionId +
                    ". Reinstalling.");

                ReinstallContent(cancellationToken);
            }
            else if (repairCost < contentSize)
            {
                DebugLogger.Log(string.Format("Repair cost {0} is smaller than content cost {1}, repairing...", repairCost, contentSize));
                IAppUpdaterStrategy repairStrategy = _strategyResolver.Create(StrategyType.Repair, Context);
                repairStrategy.Update(cancellationToken);
            }
            else
            {
                DebugLogger.Log(string.Format("Content cost {0} is smaller than repair {1}. Reinstalling.", contentSize, repairCost));
                ReinstallContent(cancellationToken);
            }

            return false;
        }

        private VersionIntegrity CheckIntegrity(
            PatchKit.Unity.Patcher.Cancellation.CancellationToken cancellationToken,
            int installedVersionId
            )
        {
            ICheckVersionIntegrityCommand checkIntegrity = _commandFactory
                .CreateCheckVersionIntegrityCommand(
                    versionId: installedVersionId,
                    context: Context,
                    isCheckingHash: CheckHashes,
                    isCheckingSize: true,
                    cancellationToken: cancellationToken);

            checkIntegrity.Prepare(_status, cancellationToken);
            checkIntegrity.Execute(cancellationToken);

            return checkIntegrity.Results;
        }

        private IEnumerable<FileIntegrity> FilesNeedFixing(VersionIntegrity results)
        {
            var missingFiles = results.Files.Where(f => f.Status == FileIntegrityStatus.MissingData);
            var invalidSizeFiles = results.Files.Where(f => f.Status == FileIntegrityStatus.InvalidSize);

            return missingFiles.Concat(invalidSizeFiles);
        }

        private long CalculateRepairCost(AppContentSummary contentSummary, IEnumerable<FileIntegrity> filesToRepair)
        {
            return filesToRepair
                .Select(f => contentSummary.Files.FirstOrDefault(e => e.Path == f.FileName))
                .Sum(f => f.Size);
        }

        private void ReinstallContent(PatchKit.Unity.Patcher.Cancellation.CancellationToken cancellationToken)
        {
            IUninstallCommand uninstall = _commandFactory.CreateUninstallCommand(Context);
            uninstall.Prepare(_status, cancellationToken);
            uninstall.Execute(cancellationToken);

            // not catching any exceptions here, because exception during content installation in this place should be fatal
            var contentStrategy = new AppUpdaterContentStrategy(Context, _status);
            contentStrategy.RepairOnError = false; // do not attempt to repair content to not cause a loop
            contentStrategy.Update(cancellationToken);
        }
    }
}