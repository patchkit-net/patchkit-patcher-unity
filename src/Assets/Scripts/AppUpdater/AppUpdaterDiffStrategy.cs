using System.Collections.Generic;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterDiffStrategy : IAppUpdaterStrategy
    {
        private struct DiffCommands
        {
            public IDownloadDiffPackageCommand DownloadDiffPackage;
            public IInstallDiffCommand InstallDiffPackage;
            public int VersionId;
        }

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdaterDiffStrategy));

        private readonly AppUpdaterContext _context;

        private bool _updateHasBeenCalled;

        public AppUpdaterDiffStrategy(AppUpdaterContext context)
        {
            AssertChecks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _context = context;
        }

        public void Update(CancellationToken cancellationToken)
        {
            AssertChecks.MethodCalledOnlyOnce(ref _updateHasBeenCalled, "Update");
            AssertChecks.ApplicationIsInstalled(_context.App);

            DebugLogger.Log("Updating with diff strategy.");

            var latestVersionId = _context.App.GetLatestVersionId();
            var currentLocalVersionId = _context.App.GetInstalledVersionId();

            DebugLogger.LogVariable(latestVersionId, "latestVersionId");
            DebugLogger.LogVariable(currentLocalVersionId, "currentLocalVersionId");

            var commandFactory = new AppUpdaterCommandFactory();

            var validateLicense = commandFactory.CreateValidateLicenseCommand(_context);
            validateLicense.Prepare(_context.StatusMonitor);

            var diffCommandsList = new List<DiffCommands>();

            for (int i = currentLocalVersionId + 1; i <= latestVersionId; i++)
            {
                DiffCommands diffCommands;

                diffCommands.VersionId = i;

                diffCommands.DownloadDiffPackage = commandFactory.CreateDownloadDiffPackageCommand(i,
                    _context);
                diffCommands.DownloadDiffPackage.Prepare(_context.StatusMonitor);

                var diffSummary = _context.App.RemoteMetaData.GetDiffSummary(i);

                diffCommands.InstallDiffPackage = commandFactory.CreateInstallDiffCommand(i, diffSummary,
                    _context.App.LocalData, _context.App.LocalMetaData, _context.App.TemporaryData);
                diffCommands.InstallDiffPackage.Prepare(_context.StatusMonitor);

                diffCommandsList.Add(diffCommands);
            }

            validateLicense.Execute(cancellationToken);

            foreach (var diffCommands in diffCommandsList)
            {
                diffCommands.DownloadDiffPackage.SetKeySecret(validateLicense.KeySecret);
                diffCommands.DownloadDiffPackage.Execute(cancellationToken);

                diffCommands.InstallDiffPackage.SetPackagePath(diffCommands.DownloadDiffPackage.PackagePath);
                diffCommands.InstallDiffPackage.Execute(cancellationToken);

                AssertChecks.ApplicationVersionEquals(_context.App, diffCommands.VersionId);
            }

            _context.App.DownloadData.Clear();
        }
    }
}