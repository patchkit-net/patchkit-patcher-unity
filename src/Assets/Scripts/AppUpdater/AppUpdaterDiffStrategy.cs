using System.Collections.Generic;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    internal class AppUpdaterDiffStrategy : IAppUpdaterStrategy
    {
        private struct DiffCommands
        {
            public IDownloadDiffPackageCommand DownloadDiffPackage;
            public IInstallDiffCommand InstallDiffPackage;
            public int VersionId;
        }

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdaterDiffStrategy));

        private readonly AppUpdaterContext _context;
        private bool _patchCalled;

        public AppUpdaterDiffStrategy(AppUpdaterContext context)
        {
            AssertChecks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _context = context;
        }

        public void Patch(CancellationToken cancellationToken)
        {
            AssertChecks.MethodCalledOnlyOnce(ref _patchCalled, "Patch");
            AssertChecks.ApplicationIsInstalled(_context.Data.LocalData);

            DebugLogger.Log("Patching with diff strategy.");

            var latestVersionId = _context.Data.RemoteData.MetaData.GetLatestVersionId();
            var currentLocalVersionId = _context.Data.LocalData.GetInstalledVersion();

            var commandFactory = new AppUpdaterCommandFactory();

            var validateLicense = commandFactory.CreateValidateLicenseCommand(_context);
            validateLicense.Prepare(_context.StatusMonitor);

            var diffCommandsList = new List<DiffCommands>();

            for (int i = currentLocalVersionId + 1; i <= latestVersionId; i++)
            {
                DiffCommands diffCommands;

                diffCommands.VersionId = i;

                diffCommands.DownloadDiffPackage = commandFactory.CreateDownloadDiffPackageCommand(latestVersionId,
                    _context);
                diffCommands.DownloadDiffPackage.Prepare(_context.StatusMonitor);

                diffCommands.InstallDiffPackage = commandFactory.CreateInstallDiffCommand(latestVersionId, _context);
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

                AssertChecks.ApplicationVersionEquals(_context.Data.LocalData, diffCommands.VersionId);
            }
        }
    }
}