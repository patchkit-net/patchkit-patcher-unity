using System.IO;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    /*
     * TODO: Content installation is not registering directories because they are not listed in content summary.
     * The only solution would be to include directories in content summary. Waits for API update.
     */
    public class InstallContentCommand : BaseAppUpdaterCommand, IInstallContentCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(InstallContentCommand));

        private readonly string _packagePath;
        private readonly string _packagePassword;
        private readonly int _versionId;
        private readonly AppContentSummary _versionContentSummary;
        private readonly ILocalDirectory _localData;
        private readonly ILocalMetaData _localMetaData;
        private readonly ITemporaryDirectory _temporaryData;

        private IGeneralStatusReporter _copyFilesStatusReporter;
        private IGeneralStatusReporter _unarchivePackageStatusReporter;

        public InstallContentCommand(string packagePath, string packagePassword, int versionId,
            AppContentSummary versionContentSummary,
            ILocalDirectory localData,
            ILocalMetaData localMetaData,
            ITemporaryDirectory temporaryData)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            // TODO: Validate the content summary.
            AssertChecks.ArgumentNotNull(localData, "localData");
            AssertChecks.ArgumentNotNull(localMetaData, "localMetaData");
            AssertChecks.ArgumentNotNull(temporaryData, "temporaryData");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(packagePath, "packagePath");
            DebugLogger.LogVariable(versionId, "versionId");

            _packagePath = packagePath;
            _packagePassword = packagePassword;
            _versionId = versionId;
            _versionContentSummary = versionContentSummary;
            _localData = localData;
            _localMetaData = localMetaData;
            _temporaryData = temporaryData;
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");

            DebugLogger.Log("Preparing content installation.");

            _localData.PrepareForWriting();
            _temporaryData.PrepareForWriting();

            double copyFilesWeight = StatusWeightHelper.GetCopyContentFilesWeight(_versionContentSummary);
            _copyFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(copyFilesWeight);

            double unarchivePackageWeight = StatusWeightHelper.GetUnarchivePackageWeight(_versionContentSummary.Size);
            _unarchivePackageStatusReporter = statusMonitor.CreateGeneralStatusReporter(unarchivePackageWeight);
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            Checks.FileExists(_packagePath);
            AssertChecks.IsTrue(_localMetaData.GetRegisteredEntries().Length == 0, "Cannot install content if previous version is still present.");

            DebugLogger.Log("Installing content.");
            
            var packageDirPath = _temporaryData.GetUniquePath();
            DebugLogger.LogVariable(packageDirPath, "packageDirPath");

            DebugLogger.Log("Creating package directory.");
            DirectoryOperations.CreateDirectory(packageDirPath);
            try
            {
                DebugLogger.Log("Unarchiving package.");

                var unarchiver = new Unarchiver(_packagePath, packageDirPath, _packagePassword);

                unarchiver.UnarchiveProgressChanged += (name, isFile, entry, amount) =>
                {
                    _unarchivePackageStatusReporter.OnProgressChanged(entry/(double) amount);
                };

                unarchiver.Unarchive(cancellationToken);

                _unarchivePackageStatusReporter.OnProgressChanged(1.0);

                DebugLogger.Log("Copying files.");

                for (int i = 0; i < _versionContentSummary.Files.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    InstallFile(_versionContentSummary.Files[i].Path, packageDirPath);

                    _copyFilesStatusReporter.OnProgressChanged((i + 1)/(double)_versionContentSummary.Files.Length);
                }

                _copyFilesStatusReporter.OnProgressChanged(1.0);
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

        private void InstallFile(string fileName, string packageDirPath)
        {
            DebugLogger.Log(string.Format("Installing file {0}", fileName));

            string sourceFilePath = Path.Combine(packageDirPath, fileName);

            if (!File.Exists(sourceFilePath))
            {
                throw new InstallerException(string.Format("Cannot find file {0} in content package.", fileName));
            }

            string filePath = _localData.Path.PathCombine(fileName);
            DirectoryOperations.CreateParentDirectory(filePath);
            FileOperations.Copy(sourceFilePath, filePath, true);
            _localMetaData.RegisterEntry(fileName, _versionId);
        }
    }
}