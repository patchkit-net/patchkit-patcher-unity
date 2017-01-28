using System.IO;
using System.Linq;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class InstallDiffCommand : BaseAppUpdaterCommand, IInstallDiffCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(InstallDiffCommand));

        private readonly int _versionId;
        private readonly AppDiffSummary _versionDiffSummary;
        private readonly ILocalData _localData;
        private readonly ILocalMetaData _localMetaData;
        private readonly ITemporaryData _temporaryData;

        private string _packagePath;
        private IGeneralStatusReporter _addFilesStatusReporter;
        private IGeneralStatusReporter _modifiedFilesStatusReporter;
        private IGeneralStatusReporter _removeFilesStatusReporter;
        private IGeneralStatusReporter _unarchivePackageStatusReporter;

        public InstallDiffCommand(int versionId,
            AppDiffSummary versionDiffSummary,
            ILocalData localData,
            ILocalMetaData localMetaData,
            ITemporaryData temporaryData)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            // TODO: Check whether version diff summary is correct
            AssertChecks.ArgumentNotNull(localData, "localData");
            AssertChecks.ArgumentNotNull(localMetaData, "localMetaData");
            AssertChecks.ArgumentNotNull(temporaryData, "temporaryData");

            _versionId = versionId;
            _versionDiffSummary = versionDiffSummary;
            _localData = localData;
            _localMetaData = localMetaData;
            _temporaryData = temporaryData;
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            DebugLogger.Log("Installing diff.");

            var packageDirPath = _temporaryData.GetUniquePath();
            DebugLogger.LogVariable(packageDirPath, "packageDirPath");

            DebugLogger.Log("Creating package directory.");
            Directory.CreateDirectory(packageDirPath);
            try
            {
                DebugLogger.Log("Unarchiving files.");

                var unarchiver = new Unarchiver(_packagePath, packageDirPath);

                unarchiver.UnarchiveProgressChanged += (name, isFile, entry, amount) =>
                {
                    _unarchivePackageStatusReporter.OnProgressChanged(entry/(double) amount);
                };

                unarchiver.Unarchive(cancellationToken);

                ProcessAddedFiles(packageDirPath, cancellationToken);
                ProcessRemovedFiles(cancellationToken);
                ProcessModifiedFiles(packageDirPath, cancellationToken);
            }
            finally
            {
                DebugLogger.Log("Deleting package directory.");
                if (Directory.Exists(packageDirPath))
                {
                    Directory.Delete(packageDirPath, true);
                }
            }
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");

            DebugLogger.Log("Preparing diff installation.");

            _localData.EnableWriteAccess();
            _temporaryData.EnableWriteAccess();

            double unarchivePackageWeight = StatusWeightHelper.GetUnarchivePackageWeight(_versionDiffSummary.Size);
            _unarchivePackageStatusReporter = statusMonitor.CreateGeneralStatusReporter(unarchivePackageWeight);

            double addFilesWeight = StatusWeightHelper.GetAddDiffFilesWeight(_versionDiffSummary);
            _addFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(addFilesWeight);

            double modifiedFilesWeight = StatusWeightHelper.GetModifyDiffFilesWeight(_versionDiffSummary);
            _modifiedFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(modifiedFilesWeight);

            double removeFilesWeight = StatusWeightHelper.GetRemoveDiffFilesWeight(_versionDiffSummary);
            _removeFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(removeFilesWeight);
        }

        public void SetPackagePath(string packagePath)
        {
            DebugLogger.Log("Setting package path.");
            DebugLogger.LogVariable(packagePath, "packagePath");

            _packagePath = packagePath;
        }

        private void ProcessRemovedFiles(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Processing removed files.");

            var removedFiles = _versionDiffSummary.RemovedFiles.Where(s => !s.EndsWith("/"));
            var removedDirectories = _versionDiffSummary.RemovedFiles.Where(s => s.EndsWith("/"));

            int counter = 0;

            foreach (var fileName in removedFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _localData.DeleteFile(fileName);
                _localMetaData.RemoveFile(fileName);

                counter++;

                _removeFilesStatusReporter.OnProgressChanged(counter/(double)_versionDiffSummary.RemovedFiles.Length);
            }

            foreach (var dirName in removedDirectories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_localData.IsDirectoryEmpty(dirName))
                {
                    _localData.DeleteDirectory(dirName);
                }

                counter++;
                _removeFilesStatusReporter.OnProgressChanged(counter/(double)_versionDiffSummary.RemovedFiles.Length);
            }
        }

        private void ProcessAddedFiles(string packageDirPath,
            CancellationToken cancellationToken)
        {
            DebugLogger.Log("Processing added files.");

            for (int i = 0; i < _versionDiffSummary.AddedFiles.Length; i++)
            {
                var fileName = _versionDiffSummary.AddedFiles[i];
                cancellationToken.ThrowIfCancellationRequested();

                if (fileName.EndsWith("/"))
                {
                    _localData.CreateDirectory(fileName);
                }
                else
                {
                    string sourceFilePath = Path.Combine(packageDirPath, fileName);

                    if (!File.Exists(sourceFilePath))
                    {
                        throw new InstallerException(string.Format("Cannot find file {0} in content package.", fileName));
                    }

                    _localData.CreateOrUpdateFile(fileName, sourceFilePath);
                    _localMetaData.AddOrUpdateFile(fileName, _versionId);
                }

                _addFilesStatusReporter.OnProgressChanged((i + 1)/(double)_versionDiffSummary.AddedFiles.Length);
            }
        }

        private void ProcessModifiedFiles(string packageDirPath,
            CancellationToken cancellationToken)
        {
            DebugLogger.Log("Processing modified files.");

            for (int i = 0; i < _versionDiffSummary.ModifiedFiles.Length; i++)
            {
                var fileName = _versionDiffSummary.ModifiedFiles[i];
                cancellationToken.ThrowIfCancellationRequested();

                if (!fileName.EndsWith("/"))
                {
                    PatchFile(fileName, packageDirPath);
                }

                _modifiedFilesStatusReporter.OnProgressChanged((i + 1)/(double)_versionDiffSummary.ModifiedFiles.Length);
            }
        }

        private void PatchFile(string fileName, string packageDirPath)
        {
            if (!_localData.FileExists(fileName) || !_localMetaData.FileExists(fileName))
            {
                throw new InstallerException(string.Format("Couldn't patch file {0} - file doesn't exists.", fileName));
            }

            AssertChecks.AreEqual(_versionId - 1, _localMetaData.GetFileVersionId(fileName),
                string.Format("Wrong file version {0}", fileName));

            string newFile = _temporaryData.GetUniquePath();

            try
            {
                var filePatcher = new FilePatcher(_localData.GetFilePath(fileName),
                    Path.Combine(packageDirPath, fileName), newFile);
                filePatcher.Patch();

                _localData.CreateOrUpdateFile(fileName, newFile);
                _localMetaData.AddOrUpdateFile(fileName, _versionId);
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