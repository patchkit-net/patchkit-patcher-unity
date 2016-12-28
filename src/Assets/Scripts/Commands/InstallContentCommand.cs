using System.IO;
using PatchKit.Api.Models;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data.Local;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Progress;
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

                LinkUnarchiverProgressReporter(unarchiver, summary);

                unarchiver.Unarchive(cancellationToken);

                var progressWeight = ProgressWeightHelper.GetCopyFilesWeight(summary.Size);
                var progressReporter = _context.ProgressMonitor.CreateGeneralProgressReporter(progressWeight);

                for (int i = 0; i < summary.Files.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    InstallFile(summary.Files[i].Path, packageDir.Path);

                    progressReporter.OnProgressChanged((i + 1)/(double) summary.Files.Length);
                }
            }
        }

        public void Prepare(IProgressMonitor progressMonitor)
        {
            throw new System.NotImplementedException();
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

        private void LinkUnarchiverProgressReporter(Unarchiver unarchiver, AppContentSummary summary)
        {
            var progressWeight = ProgressWeightHelper.GetUnarchiveWeight(summary.Size);
            var progressReporter = _context.ProgressMonitor.CreateGeneralProgressReporter(progressWeight);

            unarchiver.UnarchiveProgressChanged += (name, entry, amount) =>
            {
                progressReporter.OnProgressChanged(entry/(double)amount);
            };
        }
    }
}