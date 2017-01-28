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
            AssertChecks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _context = context;
        }

        public void Update(CancellationToken cancellationToken)
        {
            AssertChecks.MethodCalledOnlyOnce(ref _updateHasBeenCalled, "Update");

            DebugLogger.Log("Updating with content strategy.");

            var commandFactory = new Commands.AppUpdaterCommandFactory();

            var latestVersionId = _context.App.GetLatestVersionId();

            DebugLogger.LogVariable(latestVersionId, "latestVersionId");

            var latestVersionContentSummary = _context.App.RemoteMetaData.GetContentSummary(latestVersionId);

            var validateLicense = commandFactory.CreateValidateLicenseCommand(_context);
            validateLicense.Prepare(_context.StatusMonitor);

            var uninstall = commandFactory.CreateUninstallCommand(_context.App.LocalData, _context.App.LocalMetaData);
            uninstall.Prepare(_context.StatusMonitor);

            var downloadContentPackage = commandFactory.CreateDownloadContentPackageCommand(latestVersionId, _context);
            downloadContentPackage.Prepare(_context.StatusMonitor);

            var installContent = commandFactory.CreateInstallContentCommand(latestVersionId,
                latestVersionContentSummary,
                _context.App.LocalData,
                _context.App.LocalMetaData,
                _context.App.TemporaryData);
            installContent.Prepare(_context.StatusMonitor);

            validateLicense.Execute(cancellationToken);

            uninstall.Execute(cancellationToken);

            downloadContentPackage.SetKeySecret(validateLicense.KeySecret);
            downloadContentPackage.Execute(cancellationToken);

            installContent.SetPackagePath(downloadContentPackage.PackagePath);
            installContent.Execute(cancellationToken);

            _context.App.DownloadData.Clear();
        }
    }
}