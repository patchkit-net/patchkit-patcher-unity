using System.IO;
using System.Linq;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class InstallDiffCommand : BaseAppUpdaterCommand, IInstallDiffCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(InstallDiffCommand));

        private readonly string _packagePath;
        private readonly string _packagePassword;
        private readonly int _versionId;
        private readonly AppDiffSummary _versionDiffSummary;
        private readonly ILocalDirectory _localData;
        private readonly ILocalMetaData _localMetaData;
        private readonly ITemporaryDirectory _temporaryData;

        private IGeneralStatusReporter _addFilesStatusReporter;
        private IGeneralStatusReporter _modifiedFilesStatusReporter;
        private IGeneralStatusReporter _removeFilesStatusReporter;
        private IGeneralStatusReporter _unarchivePackageStatusReporter;

        public InstallDiffCommand(
            string packagePath,
            string packagePassword,
            int versionId,
            AppDiffSummary versionDiffSummary,
            ILocalDirectory localData,
            ILocalMetaData localMetaData,
            ITemporaryDirectory temporaryData)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            // TODO: Check whether version diff summary is correct
            AssertChecks.ArgumentNotNull(localData, "localData");
            AssertChecks.ArgumentNotNull(localMetaData, "localMetaData");
            AssertChecks.ArgumentNotNull(temporaryData, "temporaryData");

            _packagePath = packagePath;
            _packagePassword = packagePassword;
            _versionId = versionId;
            _versionDiffSummary = versionDiffSummary;
            _localData = localData;
            _localMetaData = localMetaData;
            _temporaryData = temporaryData;
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");

            DebugLogger.Log("Preparing diff installation.");

            _localData.PrepareForWriting();
            _temporaryData.PrepareForWriting();

            double unarchivePackageWeight = StatusWeightHelper.GetUnarchivePackageWeight(_versionDiffSummary.Size);
            _unarchivePackageStatusReporter = statusMonitor.CreateGeneralStatusReporter(unarchivePackageWeight);

            double addFilesWeight = StatusWeightHelper.GetAddDiffFilesWeight(_versionDiffSummary);
            _addFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(addFilesWeight);

            double modifiedFilesWeight = StatusWeightHelper.GetModifyDiffFilesWeight(_versionDiffSummary);
            _modifiedFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(modifiedFilesWeight);

            double removeFilesWeight = StatusWeightHelper.GetRemoveDiffFilesWeight(_versionDiffSummary);
            _removeFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(removeFilesWeight);
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            Checks.FileExists(_packagePath);

            DebugLogger.Log("Installing diff.");

            var packageDirPath = _temporaryData.GetUniquePath();
            DebugLogger.LogVariable(packageDirPath, "packageDirPath");

            DebugLogger.Log("Creating package directory.");
            DirectoryOperations.CreateDirectory(packageDirPath);
            try
            {
                DebugLogger.Log("Unarchiving files.");

                var unarchiver = new Unarchiver(_packagePath, packageDirPath, _packagePassword);

                unarchiver.UnarchiveProgressChanged += (name, isFile, entry, amount) =>
                {
                    _unarchivePackageStatusReporter.OnProgressChanged(entry/(double) amount);
                };

                unarchiver.Unarchive(cancellationToken);

                _unarchivePackageStatusReporter.OnProgressChanged(1.0);

                ProcessAddedFiles(packageDirPath, cancellationToken);
                ProcessRemovedFiles(cancellationToken);
                ProcessModifiedFiles(packageDirPath, cancellationToken);
            }
            finally
            {
                DebugLogger.Log("Deleting package directory.");
                if (Directory.Exists(packageDirPath))
                {
                    DirectoryOperations.Delete(packageDirPath, true);
                }
            }
        }

        private void ProcessRemovedFiles(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Processing removed files.");

            var files = _versionDiffSummary.RemovedFiles.Where(s => !s.EndsWith("/"));
            var directories = _versionDiffSummary.RemovedFiles.Where(s => s.EndsWith("/"));

            int counter = 0;

            foreach (var fileName in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string filePath = _localData.Path.PathCombine(fileName);

                if (File.Exists(filePath))
                {
                    FileOperations.Delete(filePath);
                }

                _localMetaData.UnregisterEntry(fileName);

                counter++;
                _removeFilesStatusReporter.OnProgressChanged(counter/(double)_versionDiffSummary.RemovedFiles.Length);
            }

            foreach (var dirName in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string dirPath = _localData.Path.PathCombine(dirName);

                if (Directory.Exists(dirPath) && DirectoryOperations.IsDirectoryEmpty(dirPath))
                {
                    DirectoryOperations.Delete(dirPath, false);
                }

                // TODO: Uncomment this after fixing directory registration in install content command
                //_localMetaData.UnregisterEntry(dirName);

                counter++;
                _removeFilesStatusReporter.OnProgressChanged(counter/(double)_versionDiffSummary.RemovedFiles.Length);
            }

            _removeFilesStatusReporter.OnProgressChanged(1.0);
        }

        private void ProcessAddedFiles(string packageDirPath,
            CancellationToken cancellationToken)
        {
            DebugLogger.Log("Processing added files.");

            for (int i = 0; i < _versionDiffSummary.AddedFiles.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entryName = _versionDiffSummary.AddedFiles[i];

                string entryPath = _localData.Path.PathCombine(entryName);

                if (entryName.EndsWith("/"))
                {
                    DirectoryOperations.CreateDirectory(entryPath);

                    // TODO: Uncomment this after fixing directory registration in install content command
                    //_localMetaData.RegisterEntry(entryName, _versionId);
                }
                else
                {
                    string sourceFilePath = Path.Combine(packageDirPath, entryName);

                    if (!File.Exists(sourceFilePath))
                    {
                        throw new InstallerException(string.Format("Cannot find file <{0}> in content package.", entryName));
                    }

                    DirectoryOperations.CreateParentDirectory(entryPath);
                    FileOperations.Copy(sourceFilePath, entryPath, true);

                    _localMetaData.RegisterEntry(entryName, _versionId);
                }

                _addFilesStatusReporter.OnProgressChanged((i + 1)/(double)_versionDiffSummary.AddedFiles.Length);
            }

            _addFilesStatusReporter.OnProgressChanged(1.0);
        }

        private void ProcessModifiedFiles(string packageDirPath,
            CancellationToken cancellationToken)
        {
            DebugLogger.Log("Processing modified files.");

            for (int i = 0; i < _versionDiffSummary.ModifiedFiles.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entryName = _versionDiffSummary.ModifiedFiles[i];
                
                if (!entryName.EndsWith("/"))
                {
                    PatchFile(entryName, packageDirPath);

                    _localMetaData.RegisterEntry(entryName, _versionId);
                }
                else
                {
                    // TODO: Uncomment this after fixing directory registration in install content command
                    //_localMetaData.RegisterEntry(entryName, _versionId);
                }

                _modifiedFilesStatusReporter.OnProgressChanged((i + 1)/(double)_versionDiffSummary.ModifiedFiles.Length);
            }

            _modifiedFilesStatusReporter.OnProgressChanged(1.0);
        }

        private void PatchFile(string fileName, string packageDirPath)
        {
            string filePath = _localData.Path.PathCombine(fileName);

            if (!File.Exists(filePath))
            {
                throw new InstallerException(string.Format("Couldn't patch file <{0}> - file doesn't exists.", fileName));
            }
            if (!_localMetaData.IsEntryRegistered(fileName))
            {
                throw new InstallerException(string.Format("Couldn't patch file <{0}> - file is not registered in meta data.", fileName));
            }

            int fileVersion = _localMetaData.GetEntryVersionId(fileName);
            if (fileVersion != _versionId - 1)
            {
                throw new InstallerException(string.Format(
                    "Couldn't patch file <{0}> - expected file with previous version ({1}) but the file version is {2}.", 
                    fileName, _versionId - 1, fileVersion));
            }

            string newFilePath = _temporaryData.GetUniquePath();

            try
            {
                var filePatcher = new FilePatcher(filePath,
                    Path.Combine(packageDirPath, fileName), newFilePath);
                filePatcher.Patch();

                FileOperations.Copy(newFilePath, filePath, true);
            }
            finally
            {
                if (File.Exists(newFilePath))
                {
                    FileOperations.Delete(newFilePath);
                }
            }
        }
    }
}