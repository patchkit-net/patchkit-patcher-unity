using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterContentStrategy : IAppUpdaterStrategy
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdaterContentStrategy));

        private readonly AppUpdaterContext _context;

        private bool _updateHasBeenCalled;

        public AppUpdaterContentStrategy(AppUpdaterContext context)
        {
            Checks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _context = context;
        }

        public void Update(CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _updateHasBeenCalled, "Update");

            DebugLogger.Log("Updating with content strategy.");

            var commandFactory = new Commands.AppUpdaterCommandFactory();

            var latestVersionId = _context.App.GetLatestVersionId();

            DebugLogger.LogVariable(latestVersionId, "latestVersionId");

            var checkDiskSpaceCommand = commandFactory.CreateCheckDiskSpaceCommandForContent(latestVersionId, _context);
            checkDiskSpaceCommand.Prepare(_context.StatusMonitor);
            checkDiskSpaceCommand.Execute(cancellationToken);

            var validateLicense = commandFactory.CreateValidateLicenseCommand(_context);
            validateLicense.Prepare(_context.StatusMonitor);
            validateLicense.Execute(cancellationToken);

            var uninstall = commandFactory.CreateUninstallCommand(_context);
            uninstall.Prepare(_context.StatusMonitor);

            var downloadContentPackage = commandFactory.CreateDownloadContentPackageCommand(latestVersionId,
                validateLicense.KeySecret, _context);
            downloadContentPackage.Prepare(_context.StatusMonitor);

            var installContent = commandFactory.CreateInstallContentCommand(latestVersionId, _context);
            installContent.Prepare(_context.StatusMonitor);

            uninstall.Execute(cancellationToken);
            downloadContentPackage.Execute(cancellationToken);
            installContent.Execute(cancellationToken);

            _context.App.DownloadDirectory.Clear();
        }
    }
}