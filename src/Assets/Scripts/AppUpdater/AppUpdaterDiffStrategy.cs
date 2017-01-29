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
            public IDownloadPackageCommand Download;
            public IInstallDiffCommand Install;
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
            validateLicense.Execute(cancellationToken);

            var diffCommandsList = new List<DiffCommands>();

            for (int i = currentLocalVersionId + 1; i <= latestVersionId; i++)
            {
                DiffCommands diffCommands;

                diffCommands.Download = commandFactory.CreateDownloadDiffPackageCommand(i, validateLicense.KeySecret,
                    _context);
                diffCommands.Download.Prepare(_context.StatusMonitor);

                diffCommands.Install = commandFactory.CreateInstallDiffCommand(i, _context);
                diffCommands.Install.Prepare(_context.StatusMonitor);

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