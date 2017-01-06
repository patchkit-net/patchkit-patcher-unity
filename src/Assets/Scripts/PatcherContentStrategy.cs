using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher
{
    internal class PatcherContentStrategy : IPatcherStrategy
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(PatcherContentStrategy));

        private readonly PatcherContext _context;
        private bool _patchCalled;

        public PatcherContentStrategy(PatcherContext context)
        {
            AssertChecks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _context = context;
        }

        public void Patch(CancellationToken cancellationToken)
        {
            AssertChecks.MethodCalledOnlyOnce(ref _patchCalled, "Patch");

            DebugLogger.Log("Patching with content strategy.");

            var commandFactory = new Commands.CommandFactory();

            var latestVersionId = _context.Data.RemoteData.MetaData.GetLatestVersionId();

            var uninstall = commandFactory.CreateUninstallCommand(_context);
            uninstall.Prepare(_context.StatusMonitor);

            var downloadContentPackage = commandFactory.CreateDownloadContentPackageCommand(latestVersionId, null,
                _context);
            downloadContentPackage.Prepare(_context.StatusMonitor);

            var installContent = commandFactory.CreateInstallContentCommand(downloadContentPackage.PackagePath,
                latestVersionId, _context);
            installContent.Prepare(_context.StatusMonitor);

            uninstall.Execute(cancellationToken);
            downloadContentPackage.Execute(cancellationToken);
            installContent.Execute(cancellationToken);
        }
    }
}