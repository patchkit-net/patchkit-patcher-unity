using System.Collections.Generic;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterDiffStrategy: IAppUpdaterStrategy
    {
        private struct DiffCommands
        {
            public IDownloadPackageCommand Download;
            public IInstallDiffCommand Install;
        }

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdaterDiffStrategy));

        private readonly AppUpdaterContext _context;

        private readonly UpdaterStatus _status;

        private bool _updateHasBeenCalled;

        public AppUpdaterDiffStrategy(AppUpdaterContext context, UpdaterStatus status)
        {

            DebugLogger.LogConstructor();
            Checks.ArgumentNotNull(context, "context");

            _context = context;
            _status = status;
        }

        public StrategyType GetStrategyType()
        {
            return StrategyType.Diff;
        }

        public void Update(CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _updateHasBeenCalled, "Update");
            Assert.ApplicationIsInstalled(_context.App);

            DebugLogger.Log("Updating with diff strategy.");

            var latestVersionId = _context.App.GetLatestVersionId();
            var currentLocalVersionId = _context.App.GetInstalledVersionId();

            DebugLogger.LogVariable(latestVersionId, "latestVersionId");
            DebugLogger.LogVariable(currentLocalVersionId, "currentLocalVersionId");

            var commandFactory = new AppUpdaterCommandFactory();
            var geolocateCommand = commandFactory.CreateGeolocateCommand();

            geolocateCommand.Prepare(_status);
            geolocateCommand.Execute(cancellationToken);

            var checkDiskSpaceCommand = commandFactory.CreateCheckDiskSpaceCommandForDiff(latestVersionId, _context);
            checkDiskSpaceCommand.Prepare(_status);
            checkDiskSpaceCommand.Execute(cancellationToken);

            var validateLicense = commandFactory.CreateValidateLicenseCommand(_context);
            validateLicense.Prepare(_status);
            validateLicense.Execute(cancellationToken);

            var diffCommandsList = new List<DiffCommands>();

            for (int i = currentLocalVersionId + 1; i <= latestVersionId; i++)
            {
                DiffCommands diffCommands;

                diffCommands.Download = commandFactory.CreateDownloadDiffPackageCommand(i, validateLicense.KeySecret,
                    geolocateCommand.CountryCode, _context);
                diffCommands.Download.Prepare(_status);

                diffCommands.Install = commandFactory.CreateInstallDiffCommand(i, _context);
                diffCommands.Install.Prepare(_status);

                diffCommandsList.Add(diffCommands);
            }

            foreach (var diffCommands in diffCommandsList)
            {
                diffCommands.Download.Execute(cancellationToken);
                diffCommands.Install.Execute(cancellationToken);
            }

            _context.App.DownloadDirectory.Clear();
        }
    }
}