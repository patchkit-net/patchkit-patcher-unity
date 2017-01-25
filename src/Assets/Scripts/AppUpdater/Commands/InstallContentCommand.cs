using System.IO;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class InstallContentCommand : BaseAppUpdaterCommand, IInstallContentCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(InstallContentCommand));

        private readonly int _versionId;
        private readonly AppContentSummary _versionContentSummary;
        private readonly ILocalData _localData;
        private readonly ILocalMetaData _localMetaData;
        private readonly ITemporaryData _temporaryData;

        private string _packagePath;
        private IGeneralStatusReporter _copyFilesStatusReporter;
        private IGeneralStatusReporter _unarchivePackageStatusReporter;

        public InstallContentCommand(int versionId,
            AppContentSummary versionContentSummary,
            ILocalData localData,
            ILocalMetaData localMetaData,
            ITemporaryData temporaryData)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            // TODO: Check whether version content summary is correct
            AssertChecks.ArgumentNotNull(localData, "localData");
            AssertChecks.ArgumentNotNull(localMetaData, "localMetaData");
            AssertChecks.ArgumentNotNull(temporaryData, "temporaryData");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(versionId, "versionId");

            _versionId = versionId;
            _versionContentSummary = versionContentSummary;
            _localData = localData;
            _localMetaData = localMetaData;
            _temporaryData = temporaryData;
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            AssertChecks.IsTrue(_localMetaData.GetFileNames().Length == 0, "Cannot install content if previous version is still present.");

            base.Execute(cancellationToken);

            DebugLogger.Log("Installing content.");
            
            var packageDirPath = _temporaryData.GetUniquePath();
            Directory.CreateDirectory(packageDirPath);
            try
            {
                DebugLogger.Log("Unarchiving package.");

                var unarchiver = new Unarchiver(_packagePath, packageDirPath);

                unarchiver.UnarchiveProgressChanged += (name, isFile, entry, amount) =>
                {
                    _unarchivePackageStatusReporter.OnProgressChanged(entry/(double) amount);
                };

                unarchiver.Unarchive(cancellationToken);

                DebugLogger.Log("Copying files.");

                for (int i = 0; i < _versionContentSummary.Files.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    InstallFile(_versionContentSummary.Files[i].Path, packageDirPath);

                    _copyFilesStatusReporter.OnProgressChanged((i + 1)/(double)_versionContentSummary.Files.Length);
                }
            }
            finally
            {
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

            DebugLogger.Log("Preparing content installation.");

            _localData.EnableWriteAccess();

            double copyFilesWeight = StatusWeightHelper.GetCopyContentFilesWeight(_versionContentSummary);
            _copyFilesStatusReporter = statusMonitor.CreateGeneralStatusReporter(copyFilesWeight);

            double unarchivePackageWeight = StatusWeightHelper.GetUnarchivePackageWeight(_versionContentSummary.Size);
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

            _localData.CreateOrUpdateFile(fileName, sourceFilePath);
            _localMetaData.AddOrUpdateFile(fileName, _versionId);
        }
    }
}