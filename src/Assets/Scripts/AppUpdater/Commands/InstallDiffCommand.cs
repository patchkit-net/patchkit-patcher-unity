using System.IO;
using System.Linq;
using PatchKit.Api.Models;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class InstallDiffCommand : BaseAppUpdaterCommand, IInstallDiffCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(InstallDiffCommand));

        private readonly int _versionId;
        private readonly AppUpdaterContext _context;

        private string _packagePath;
        private AppDiffSummary _diffSummary;
        private IGeneralStatusReporter _addFilesStatusReporter;
        private IGeneralStatusReporter _modifiedFilesStatusReporter;
        private IGeneralStatusReporter _removeFilesStatusReporter;
        private IGeneralStatusReporter _unarchivePackageStatusReporter;

        public InstallDiffCommand(int versionId, AppUpdaterContext context)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            AssertChecks.ArgumentNotNull(context, "context");

            _versionId = versionId;
            _context = context;
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            DebugLogger.Log("Installing diff.");

            using (var packageDir = new TemporaryDirectory(_context.App.LocalData.TemporaryData.GetUniquePath()))
            {
                DebugLogger.Log("Unarchiving files.");

                var unarchiver = new Unarchiver(_packagePath, packageDir.Path);

                unarchiver.UnarchiveProgressChanged += (name, isFile, entry, amount) =>
                {
                    _unarchivePackageStatusReporter.OnProgressChanged(entry / (double)amount);
                };

                unarchiver.Unarchive(cancellationToken);

                ProcessAddedFiles(packageDir.Path, cancellationToken);
                ProcessRemovedFiles(cancellationToken);
                ProcessModifiedFiles(packageDir.Path, cancellationToken);
            }
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");

            DebugLogger.Log("Preparing diff installation.");

            _context.App.LocalData.EnableWriteAccess();

            _diffSummary = _context.App.RemoteData.MetaData.GetDiffSummary(_versionId);

            double unarchivePackageWeight = StatusWeightHelper.GetUnarchivePackageWeight(_diffSummary.Size);
            _unarchivePackageStatusReporter = statusMonitor.CreateGeneralStatusReporter(unarchivePackageWeight);

            double addFilesWeight = StatusWeightHelper.GetAddDiffFilesWeight(_diffSummary);
            _addFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(addFilesWeight);

            double modifiedFilesWeight = StatusWeightHelper.GetModifyDiffFilesWeight(_diffSummary);
            _modifiedFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(modifiedFilesWeight);

            double removeFilesWeight = StatusWeightHelper.GetRemoveDiffFilesWeight(_diffSummary);
            _removeFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(removeFilesWeight);
        }

        public void SetPackagePath(string packagePath)
        {
            _packagePath = packagePath;
        }

        private void ProcessRemovedFiles(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Processing removed files.");

            var removedFiles = _diffSummary.RemovedFiles.Where(s => !s.EndsWith("/"));
            var removedDirectories = _diffSummary.RemovedFiles.Where(s => s.EndsWith("/"));

            int counter = 0;

            foreach (var fileName in removedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _context.App.LocalData.DeleteFile(fileName);
                _context.App.LocalData.MetaData.RemoveFile(fileName);

                counter++;

                _removeFilesStatusReporter.OnProgressChanged(counter/(double)_diffSummary.RemovedFiles.Length);
            }

            foreach (var dirName in removedDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_context.App.LocalData.IsDirectoryEmpty(dirName))
                {
                    _context.App.LocalData.DeleteDirectory(dirName);
                }

                counter++;
                _removeFilesStatusReporter.OnProgressChanged(counter/(double)_diffSummary.RemovedFiles.Length);
            }
        }

        private void ProcessAddedFiles(string packageDirPath,
            CancellationToken cancellationToken)
        {
            DebugLogger.Log("Processing added files.");

            for (int i = 0; i < _diffSummary.AddedFiles.Length; i++)
            {
                var fileName = _diffSummary.AddedFiles[i];
                cancellationToken.ThrowIfCancellationRequested();

                if (fileName.EndsWith("/"))
                {
                    _context.App.LocalData.CreateDirectory(fileName);
                }
                else
                {
                    string sourceFilePath = Path.Combine(packageDirPath, fileName);

                    if (!File.Exists(sourceFilePath))
                    {
                        throw new InstallerException(string.Format("Cannot find file {0} in content package.", fileName));
                    }

                    _context.App.LocalData.CreateOrUpdateFile(fileName, sourceFilePath);
                    _context.App.LocalData.MetaData.AddOrUpdateFile(fileName, _versionId);
                }

                _addFilesStatusReporter.OnProgressChanged((i + 1)/(double)_diffSummary.AddedFiles.Length);
            }
        }

        private void ProcessModifiedFiles(string packageDirPath,
            CancellationToken cancellationToken)
        {
            DebugLogger.Log("Processing modified files.");

            for (int i = 0; i < _diffSummary.ModifiedFiles.Length; i++)
            {
                var fileName = _diffSummary.ModifiedFiles[i];
                cancellationToken.ThrowIfCancellationRequested();

                if (!fileName.EndsWith("/"))
                {
                    PatchFile(fileName, packageDirPath);
                }

                _modifiedFilesStatusReporter.OnProgressChanged((i + 1)/(double)_diffSummary.ModifiedFiles.Length);
            }
        }

        private void PatchFile(string fileName, string packageDirPath)
        {
            if (!_context.App.LocalData.FileExists(fileName) || !_context.App.LocalData.MetaData.FileExists(fileName))
            {
                throw new InstallerException(string.Format("Couldn't patch file {0} - file doesn't exists.", fileName));
            }

            AssertChecks.AreEqual(_versionId - 1, _context.App.LocalData.MetaData.GetFileVersion(fileName),
                string.Format("Wrong file version {0}", fileName));

            string newFile = _context.App.LocalData.TemporaryData.GetUniquePath();

            try
            {
                var filePatcher = new FilePatcher(_context.App.LocalData.GetFilePath(fileName),
                    Path.Combine(packageDirPath, fileName), newFile);
                filePatcher.Patch();

                _context.App.LocalData.CreateOrUpdateFile(fileName, newFile);
                _context.App.LocalData.MetaData.AddOrUpdateFile(fileName, _versionId);
            }
            finally
            {
                if (File.Exists(newFile))
                {
                    File.Delete(newFile);
                }
            }
        }
    }
}