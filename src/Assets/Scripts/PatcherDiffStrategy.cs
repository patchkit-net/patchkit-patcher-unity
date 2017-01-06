using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Commands;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher
{
    internal class PatcherDiffStrategy : IPatcherStrategy
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(PatcherDiffStrategy));

        private readonly PatcherContext _context;
        private bool _patchCalled;

        public PatcherDiffStrategy(PatcherContext context)
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

            var commandFactory = new CommandFactory();

            var latestVersionId = _context.Data.RemoteData.MetaData.GetLatestVersionId();
            var currentLocalVersionId = _context.Data.LocalData.GetInstalledVersion();

            //TODO: Prepare commands
            for (int i = currentLocalVersionId + 1; i <= latestVersionId; i++)
            {
                var downloadDiffPackage = commandFactory.CreateDownloadContentPackageCommand(latestVersionId, null,
                    _context);
                downloadDiffPackage.Execute(cancellationToken);

                var installDiff = commandFactory.CreateInstallContentCommand(downloadDiffPackage.PackagePath,
                    latestVersionId, _context);
                installDiff.Execute(cancellationToken);

                AssertChecks.ApplicationVersionEquals(_context.Data.LocalData, i);
            }
        }
    }
}