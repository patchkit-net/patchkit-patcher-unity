using System.IO;
using PatchKit.Api.Models;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data.Local;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.Commands
{
    internal class InstallContentCommand : IInstallContentCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(InstallContentCommand));

        private readonly int _versionId;
        private readonly string _packagePath;
        private readonly PatcherContext _context;

        private AppContentSummary _versionSummary;
        private IGeneralStatusReporter _copyFilesStatusReporter;
        private IGeneralStatusReporter _unarchivePackageStatusReporter;

        public InstallContentCommand(string packagePath, int versionId, PatcherContext context)
        {
            _versionId = versionId;
            _packagePath = packagePath;
            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Installing content.");

            Assert.IsTrue(_context.Data.LocalData.MetaData.GetFileNames().Length == 0, "Cannot install content if previous version is still present.");

            using (var packageDir = new TemporaryDirectory(_context.Data.LocalData.TemporaryData.GetUniquePath()))
            {
                DebugLogger.Log("Unarchiving package.");

                var unarchiver = new Unarchiver(_packagePath, packageDir.Path);

                unarchiver.UnarchiveProgressChanged += (name, entry, amount) =>
                {
                    _unarchivePackageStatusReporter.OnProgressChanged(entry / (double)amount);
                };

                unarchiver.Unarchive(cancellationToken);

                DebugLogger.Log("Copying files.");

                for (int i = 0; i < _versionSummary.Files.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    InstallFile(_versionSummary.Files[i].Path, packageDir.Path);

                    _copyFilesStatusReporter.OnProgressChanged((i + 1)/(double) _versionSummary.Files.Length);
                }
            }
        }

        public void Prepare(IStatusMonitor statusMonitor)
        {
            DebugLogger.Log("Preparing content installation.");

            _versionSummary = _context.Data.RemoteData.MetaData.GetContentSummary(_versionId);

            double copyFilesWeight = StatusWeightHelper.GetCopyFilesWeight(_versionSummary.Size);
            _copyFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(copyFilesWeight);

            double unarchivePackageWeight = StatusWeightHelper.GetUnarchivePackageWeight(_versionSummary.Size);
            _unarchivePackageStatusReporter = statusMonitor.CreateGeneralStatusReporter(unarchivePackageWeight);
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