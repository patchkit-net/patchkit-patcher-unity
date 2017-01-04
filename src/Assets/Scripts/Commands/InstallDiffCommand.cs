using System.IO;
using System.Linq;
using PatchKit.Api.Models;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data.Local;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Diff;
using PatchKit.Unity.Patcher.Status;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.Commands
{
    internal class InstallDiffCommand : IInstallDiffCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(InstallDiffCommand));

        private readonly string _packagePath;
        private readonly int _versionId;
        private readonly PatcherContext _context;

        private AppContentSummary _versionSummary;
        private IGeneralStatusReporter _copyFilesStatusReporter;
        private IGeneralStatusReporter _patchFilesStatusReporter;
        private IGeneralStatusReporter _removeFilesStatusReporter;
        private IGeneralStatusReporter _unarchivePackageStatusReporter;

        public InstallDiffCommand(string packagePath, int versionId, PatcherContext context)
        {
            _packagePath = packagePath;
            _versionId = versionId;
            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            var summary = _context.Data.RemoteData.MetaData.GetDiffSummary(_versionId);

            using (var packageDir = new TemporaryDirectory(_context.Data.LocalData.TemporaryData.GetUniquePath()))
            {
                var unarchiver = new Unarchiver(_packagePath, packageDir.Path);

                LinkUnarchiverProgressReporter(unarchiver, summary);

                unarchiver.Unarchive(cancellationToken);

                ProcessAddedFiles(summary, packageDir.Path, cancellationToken);
                ProcessRemovedFiles(summary, cancellationToken);
                ProcessModifiedFiles(summary, packageDir.Path, cancellationToken);
            }
        }

        public void Prepare(IStatusMonitor statusMonitor)
        {
            throw new System.NotImplementedException();
        }

        private void ProcessRemovedFiles(AppDiffSummary summary, CancellationToken cancellationToken)
        {
            // TODO: Calculate size of removed files.
            var progressWeight = ProgressWeightHelper.GetRemoveFilesWeight(summary.Size);
            var progressReporter = _context.StatusMonitor.CreateGeneralProgressReporter(progressWeight);

            var removedFiles = summary.RemovedFiles.Where(s => !s.EndsWith("/"));
            var removedDirectories = summary.RemovedFiles.Where(s => s.EndsWith("/"));

            int counter = 0;

            foreach (var fileName in removedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _context.Data.LocalData.DeleteFile(fileName);
                _context.Data.LocalData.MetaData.RemoveFile(fileName);

                counter++;
                progressReporter.OnProgressChanged(counter/(double) summary.RemovedFiles.Length);
            }

            foreach (var dirName in removedDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_context.Data.LocalData.IsDirectoryEmpty(dirName))
                {
                    _context.Data.LocalData.DeleteDirectory(dirName);
                }

                counter++;
                progressReporter.OnProgressChanged(counter/(double) summary.RemovedFiles.Length);
            }
        }

        private void ProcessAddedFiles(AppDiffSummary summary, string packageDirPath,
            CancellationToken cancellationToken)
        {
            // TODO: Calculate size of added files.
            var progressWeight = ProgressWeightHelper.GetCopyFilesWeight(summary.Size);
            var progressReporter = _context.StatusMonitor.CreateGeneralProgressReporter(progressWeight);

            for (int i = 0; i < summary.AddedFiles.Length; i++)
            {
                var fileName = summary.AddedFiles[i];
                cancellationToken.ThrowIfCancellationRequested();

                if (fileName.EndsWith("/"))
                {
                    _context.Data.LocalData.CreateDirectory(fileName);
                }
                else
                {
                    string sourceFilePath = Path.Combine(packageDirPath, fileName);

                    if (!File.Exists(sourceFilePath))
                    {
                        throw new InstallerException(string.Format("Cannot find file {0} in content package.", fileName));
                    }

                    _context.Data.LocalData.CreateOrUpdateFile(fileName, sourceFilePath);
                    _context.Data.LocalData.MetaData.AddOrUpdateFile(fileName, _versionId);
                }

                progressReporter.OnProgressChanged((i + 1)/(double) summary.AddedFiles.Length);
            }
        }

        private void ProcessModifiedFiles(AppDiffSummary summary, string packageDirPath,
            CancellationToken cancellationToken)
        {
            // TODO: Calculate size of added files.
            var progressWeight = ProgressWeightHelper.GetPatchFilesWeight(summary.Size);
            var progressReporter = _context.StatusMonitor.CreateGeneralProgressReporter(progressWeight);

            for (int i = 0; i < summary.ModifiedFiles.Length; i++)
            {
                var fileName = summary.ModifiedFiles[i];
                cancellationToken.ThrowIfCancellationRequested();

                if (!fileName.EndsWith("/"))
                {
                    PatchFile(fileName, packageDirPath);
                }

                progressReporter.OnProgressChanged((i + 1)/(double) summary.AddedFiles.Length);
            }
        }

        private void PatchFile(string fileName, string packageDirPath)
        {
            if (!_context.Data.LocalData.FileExists(fileName) || !_context.Data.LocalData.MetaData.FileExists(fileName))
            {
                throw new InstallerException(string.Format("Couldn't patch file {0} - file doesn't exists.", fileName));
            }

            Assert.AreEqual(_versionId - 1, _context.Data.LocalData.MetaData.GetFileVersion(fileName),
                string.Format("Wrong file version {0}", fileName));

            string newFile = _context.Data.LocalData.TemporaryData.GetUniquePath();

            try
            {
                var filePatcher = new FilePatcher(_context.Data.LocalData.GetFilePath(fileName),
                    Path.Combine(packageDirPath, fileName), newFile);
                filePatcher.Patch();

                _context.Data.LocalData.CreateOrUpdateFile(fileName, newFile);
                _context.Data.LocalData.MetaData.AddOrUpdateFile(fileName, _versionId);
            }
            finally
            {
                if (File.Exists(newFile))
                {
                    File.Delete(newFile);
                }
            }
        }

        private void LinkUnarchiverProgressReporter(Unarchiver unarchiver, AppDiffSummary summary)
        {
            var progressWeight = ProgressWeightHelper.GetUnarchiveWeight(summary.Size);
            var progressReporter = _context.StatusMonitor.CreateGeneralProgressReporter(progressWeight);

            unarchiver.UnarchiveProgressChanged +=
                (name, entry, amount) => { progressReporter.OnProgressChanged(entry/(double) amount); };
        }
    }
}