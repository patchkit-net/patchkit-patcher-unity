using System;
using System.Diagnostics;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterContentStrategy: IAppUpdaterStrategy
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdaterContentStrategy));

        private readonly AppUpdaterContext _context;

        private readonly UpdaterStatus _status;

        private bool _updateHasBeenCalled;

        public bool RepairOnError { get; set; }

        public AppUpdaterContentStrategy(AppUpdaterContext context, UpdaterStatus status)
        {
            Checks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _context = context;
            _status = status;

            // defaults
            RepairOnError = true;
        }

        public StrategyType GetStrategyType()
        {
            return StrategyType.Content;
        }

        public void Update(CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _updateHasBeenCalled, "Update");

            DebugLogger.Log("Updating with content strategy.");

            var commandFactory = new Commands.AppUpdaterCommandFactory();
            var geolocateCommand = commandFactory.CreateGeolocateCommand();

            geolocateCommand.Prepare(_status, cancellationToken);
            geolocateCommand.Execute(cancellationToken);

            var latestVersionId = _context.App.GetLatestVersionId(true, cancellationToken);

            DebugLogger.LogVariable(latestVersionId, "latestVersionId");

            var checkDiskSpaceCommand = commandFactory.CreateCheckDiskSpaceCommandForContent(latestVersionId, _context, cancellationToken);
            checkDiskSpaceCommand.Prepare(_status, cancellationToken);
            checkDiskSpaceCommand.Execute(cancellationToken);

            var validateLicense = commandFactory.CreateValidateLicenseCommand(_context);
            validateLicense.Prepare(_status, cancellationToken);
            validateLicense.Execute(cancellationToken);

            var uninstall = commandFactory.CreateUninstallCommand(_context);
            uninstall.Prepare(_status, cancellationToken);

            var resource = _context.App.RemoteData.GetContentPackageResource(latestVersionId, validateLicense.KeySecret, geolocateCommand.CountryCode, cancellationToken);

            var downloadContentPackage = commandFactory.CreateDownloadContentPackageCommand(latestVersionId,
                validateLicense.KeySecret, geolocateCommand.CountryCode, _context, cancellationToken);
            downloadContentPackage.Prepare(_status, cancellationToken);

            var installContent = commandFactory.CreateInstallContentCommand(latestVersionId, _context, cancellationToken);
            installContent.Prepare(_status, cancellationToken);

            uninstall.Execute(cancellationToken);

            var downloadStopwatch = new Stopwatch();
            var optionalParams = new PatcherStatistics.OptionalParams 
            {
                VersionId = latestVersionId,
                Size = resource.Size,
            };

            Func<PatcherStatistics.OptionalParams> timedParams = () => new PatcherStatistics.OptionalParams {
                VersionId = optionalParams.VersionId,
                Size = optionalParams.Size,
                Time = downloadStopwatch.Elapsed.Seconds,
            };

            try
            {
                PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.ContentDownloadStarted, optionalParams);
                downloadStopwatch.Start();
                downloadContentPackage.Execute(cancellationToken);
                PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.ContentDownloadSucceeded, timedParams());
            }
            catch (OperationCanceledException)
            {
                PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.ContentDownloadCanceled, timedParams());
                throw;
            }
            catch (Exception)
            {
                PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.ContentDownloadFailed, timedParams());
                throw;
            }

            installContent.Execute(cancellationToken);

            if (installContent.NeedRepair && RepairOnError)
            {
                DebugLogger.Log("Content installed with errors, requesting repair");

                var appRepairer = new AppRepairer(_context, _status);
                appRepairer.CheckHashes = true;

                if (!appRepairer.Perform(cancellationToken))
                {
                    throw new CannotRepairDiskFilesException("Failed to validate/repair disk files");
                }
            }

            _context.App.DownloadDirectory.Clear();
        }
    }
}