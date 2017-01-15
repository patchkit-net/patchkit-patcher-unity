using System.IO;
using PatchKit.Api.Models;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class InstallContentCommand : IInstallContentCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(InstallContentCommand));

        private readonly int _versionId;
        private readonly AppUpdaterContext _context;

        private string _packagePath;
        private AppContentSummary _versionSummary;
        private IGeneralStatusReporter _copyFilesStatusReporter;
        private IGeneralStatusReporter _unarchivePackageStatusReporter;

        public InstallContentCommand(int versionId, AppUpdaterContext context)
        {
            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(versionId, "versionId");

            Checks.ArgumentValidVersionId(versionId, "versionId");
            Assert.IsNotNull(context, "context");

            _versionId = versionId;
            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Installing content.");

            Assert.IsTrue(_context.App.LocalData.MetaData.GetFileNames().Length == 0, "Cannot install content if previous version is still present.");

            using (var packageDir = new TemporaryDirectory(_context.App.LocalData.TemporaryData.GetUniquePath()))
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

            _versionSummary = _context.App.RemoteData.MetaData.GetContentSummary(_versionId);

            double copyFilesWeight = StatusWeightHelper.GetCopyContentFilesWeight(_versionSummary);
            _copyFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(copyFilesWeight);

            double unarchivePackageWeight = StatusWeightHelper.GetUnarchivePackageWeight(_versionSummary.Size);
            _unarchivePackageStatusReporter = statusMonitor.CreateGeneralStatusReporter(unarchivePackageWeight);
        }

        public void SetPackagePath(string packagePath)
        {
            _packagePath = packagePath;
        }

        private void InstallFile(string fileName, string packageDirPath)
        {
            DebugLogger.Log(string.Format("Installing file {0}", fileName));

            string sourceFilePath = Path.Combine(packageDirPath, fileName);

            if (!File.Exists(sourceFilePath))
            {
                throw new InstallerException(string.Format("Cannot find file {0} in content package.", fileName));
            }

            _context.App.LocalData.CreateOrUpdateFile(fileName, sourceFilePath);
            _context.App.LocalData.MetaData.AddOrUpdateFile(fileName, _versionId);
        }
    }
}