using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher
{
    internal class PatcherContentStrategy : IPatcherStrategy
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(PatcherContentStrategy));

        private readonly PatcherContext _context;

        public PatcherContentStrategy(PatcherContext context)
        {
            DebugLogger.LogConstructor();

            Assert.IsNotNull(context, "context");

            _context = context;
        }

        public void Patch(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Patching with content strategy.");

            var commandFactory = new Commands.CommandFactory();

            var uninstall = commandFactory.CreateUninstallCommand(_context);
            uninstall.Execute(cancellationToken);

            var latestVersionId = _context.Data.RemoteData.MetaData.GetLatestVersionId();

            var downloadContentPackage = commandFactory.CreateDownloadContentPackageCommand(latestVersionId, null,
                _context);
            downloadContentPackage.Execute(cancellationToken);

            var installContent = commandFactory.CreateInstallContentCommand(downloadContentPackage.PackagePath,
                latestVersionId, _context);
            installContent.Execute(cancellationToken);
        }
    }
}