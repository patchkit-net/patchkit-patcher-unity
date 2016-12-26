using System.IO;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data.Local;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.Commands
{
    internal class InstallContentCommand : IInstallContentCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(InstallContentCommand));

        private readonly int _versionId;
        private readonly string _packagePath;
        private readonly PatcherContext _context;

        public InstallContentCommand(string packagePath, int versionId, PatcherContext context)
        {
            _versionId = versionId;
            _packagePath = packagePath;
            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            Assert.IsTrue(_context.Data.LocalData.MetaData.GetFileNames().Length == 0, "Cannot install content if previous version is still present.");

            var summary = _context.Data.RemoteData.MetaData.GetContentSummary(_versionId);

            using (var packageDir = new TemporaryDirectory(_context.Data.LocalData.TemporaryData.GetUniquePath()))
            {
                var unarchiver = new Unarchiver(_packagePath, packageDir.Path);
                unarchiver.Unarchive(cancellationToken);

                foreach (var file in summary.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    InstallFile(file.Path, packageDir.Path);
                }
            }
        }

        private void InstallFile(string fileName, string packageDirPath)
        {
            DebugLogger.Log(string.Format("Installing file {0}", fileName));

            string sourceFilePath = Path.Combine(packageDirPath, fileName);

            if (!File.Exists(sourceFilePath))
            {
                throw new InstallerException(string.Format("Cannot find file {0} in content package.", fileName));
            }

            _context.Data.LocalData.CreateOrUpdateFile(fileName, sourceFilePath);
            _context.Data.LocalData.MetaData.AddOrUpdateFile(fileName, _versionId);
        }
    }
}