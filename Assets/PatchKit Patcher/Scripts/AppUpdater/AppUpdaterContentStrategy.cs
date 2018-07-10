using System.Diagnostics;
using PatchKit.Unity.Patcher.AppUpdater.Status;
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

        public AppUpdaterContentStrategy(AppUpdaterContext context, UpdaterStatus status)
        {
            Checks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _context = context;
            _status = status;
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

            geolocateCommand.Prepare(_status);
            geolocateCommand.Execute(cancellationToken);

            var latestVersionId = _context.App.GetLatestVersionId();

            DebugLogger.LogVariable(latestVersionId, "latestVersionId");

            var checkDiskSpaceCommand = commandFactory.CreateCheckDiskSpaceCommandForContent(latestVersionId, _context);
            checkDiskSpaceCommand.Prepare(_status);
            checkDiskSpaceCommand.Execute(cancellationToken);

            var validateLicense = commandFactory.CreateValidateLicenseCommand(_context);
            validateLicense.Prepare(_status);
            validateLicense.Execute(cancellationToken);

            var uninstall = commandFactory.CreateUninstallCommand(_context);
            uninstall.Prepare(_status);

            var resource = _context.App.RemoteData.GetContentPackageResource(latestVersionId, validateLicense.KeySecret, geolocateCommand.CountryCode);

            var downloadContentPackage = commandFactory.CreateDownloadContentPackageCommand(latestVersionId,
                validateLicense.KeySecret, geolocateCommand.CountryCode, _context);
            downloadContentPackage.Prepare(_status);

            var installContent = commandFactory.CreateInstallContentCommand(latestVersionId, _context);
            installContent.Prepare(_status);

            uninstall.Execute(cancellationToken);

            var downloadStopwatch = new Stopwatch();
            var optionalParams = new PatcherStatistics.OptionalParams 
            {
                VersionId = latestVersionId,
                Size = resource.Size,
            };

            System.Func<PatcherStatistics.OptionalParams> timedParams = () => new PatcherStatistics.OptionalParams {
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
            catch (System.OperationCanceledException)
            {
                PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.ContentDownloadCanceled, timedParams());
                throw;
            }
            catch (System.Exception)
            {
                PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.ContentDownloadFailed, timedParams());
                throw;
            }

            installContent.Execute(cancellationToken);

            _context.App.DownloadDirectory.Clear();
        }
    }
}