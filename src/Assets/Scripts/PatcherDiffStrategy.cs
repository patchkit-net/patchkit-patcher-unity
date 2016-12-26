using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Commands;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher
{
    internal class PatcherDiffStrategy : IPatcherStrategy
    {
        private readonly PatcherContext _context;

        public PatcherDiffStrategy(PatcherContext context)
        {
            _context = context;
        }

        public void Patch(CancellationToken cancellationToken)
        {
            Assert.IsTrue(_context.Data.LocalData.IsInstalled());

            var commandFactory = new CommandFactory();

            var latestVersionId = _context.Data.RemoteData.MetaData.GetLatestVersionId();
            var currentLocalVersionId = _context.Data.LocalData.GetInstalledVersion();

            for (int i = currentLocalVersionId + 1; i <= latestVersionId; i++)
            {
                var downloadDiffPackage = commandFactory.CreateDownloadContentPackageCommand(latestVersionId, null,
                    _context);
                downloadDiffPackage.Execute(cancellationToken);

                var installDiff = commandFactory.CreateInstallContentCommand(downloadDiffPackage.PackagePath,
                    latestVersionId, _context);
                installDiff.Execute(cancellationToken);

                Assert.IsTrue(_context.Data.LocalData.IsInstalled());
                Assert.AreEqual(i, _context.Data.LocalData.GetInstalledVersion());
            }
        }
    }
}