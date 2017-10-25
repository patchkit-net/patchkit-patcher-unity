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
        private const string Suffix = "_"; // FIX: bug #714
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(InstallContentCommand));

        private readonly string _packagePath;
        private readonly string _packageMetaPath;
        private readonly string _packagePassword;
        private readonly int _versionId;
        private readonly AppContentSummary _versionContentSummary;
        private readonly ILocalDirectory _localData;
        private readonly ILocalMetaData _localMetaData;
        private readonly ITemporaryDirectory _temporaryData;

        private IGeneralStatusReporter _copyFilesStatusReporter;
        private IGeneralStatusReporter _unarchivePackageStatusReporter;
        private Pack1Meta _pack1Meta;

        public InstallContentCommand(string packagePath, string packageMetaPath, string packagePassword, int versionId,
            AppContentSummary versionContentSummary, ILocalDirectory localData, ILocalMetaData localMetaData,
            ITemporaryDirectory temporaryData)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            // TODO: Validate the content summary.
            Checks.ArgumentNotNull(localData, "localData");
            Checks.ArgumentNotNull(localMetaData, "localMetaData");
            Checks.ArgumentNotNull(temporaryData, "temporaryData");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(packagePath, "packagePath");
            DebugLogger.LogVariable(versionId, "versionId");

            _packagePath = packagePath;
            _packageMetaPath = packageMetaPath;
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

            Checks.ArgumentNotNull(statusMonitor, "statusMonitor");

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
            Assert.IsTrue(_localMetaData.GetRegisteredEntries().Length == 0,
                "Cannot install content if previous version is still present.");

            if (_versionContentSummary.CompressionMethod == "pack1")
            {
                Assert.IsTrue(File.Exists(_packageMetaPath),
                    "Compression method is pack1, but meta file does not exist");

                DebugLogger.Log("Parsing package meta file");
                _pack1Meta = Pack1Meta.ParseFromFile(_packageMetaPath);
                DebugLogger.Log("Package meta file parsed succesfully");
            }

            DebugLogger.Log("Installing content.");
            
            var packageDirPath = _temporaryData.GetUniquePath();
            DebugLogger.LogVariable(packageDirPath, "destinationDir");

            DebugLogger.Log("Creating package directory.");
            DirectoryOperations.CreateDirectory(packageDirPath);
            try
            {
                DebugLogger.Log("Unarchiving package.");

                string usedSuffix;
                IUnarchiver unarchiver = CreateUnrachiver(packageDirPath, out usedSuffix);

                _unarchivePackageStatusReporter.OnProgressChanged(0.0, "Unarchiving package...");
                
                unarchiver.UnarchiveProgressChanged += (name, isFile, entry, amount) =>
                {
                    _unarchivePackageStatusReporter.OnProgressChanged(entry/(double) amount, "Unarchiving package...");
                };

                unarchiver.Unarchive(cancellationToken);

                _unarchivePackageStatusReporter.OnProgressChanged(1.0, string.Empty);

                DebugLogger.Log("Copying files.");

                _copyFilesStatusReporter.OnProgressChanged(0.0, "Installing package...");
                
                for (int i = 0; i < _versionContentSummary.Files.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    InstallFile(_versionContentSummary.Files[i].Path, packageDirPath, usedSuffix);

                    _copyFilesStatusReporter.OnProgressChanged((i + 1)/(double)_versionContentSummary.Files.Length, "Installing package...");
                }

                _copyFilesStatusReporter.OnProgressChanged(1.0, string.Empty);
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

        private IUnarchiver CreateUnrachiver(string destinationDir, out string usedSuffix)
        {
            switch (_versionContentSummary.CompressionMethod)
            {
                case "zip":
                    usedSuffix = string.Empty;
                    return new ZipUnarchiver(_packagePath, destinationDir, _packagePassword);
                case "pack1":
                    usedSuffix = Suffix;
                    return new Pack1Unarchiver(_packagePath, _pack1Meta, destinationDir, _packagePassword, Suffix);
                default:
                    throw new InstallerException(string.Format("Unknown compression method: {0}",
                        _versionContentSummary.CompressionMethod));
            }
        }

        private void InstallFile(string fileName, string packageDirPath, string suffix)
        {
            DebugLogger.Log(string.Format("Installing file {0}", fileName+suffix));

            string sourceFilePath = Path.Combine(packageDirPath, fileName+suffix);

            if (!File.Exists(sourceFilePath))
            {
                throw new InstallerException(string.Format("Cannot find file {0} in content package.", fileName));
            }

            string destinationFilePath = _localData.Path.PathCombine(fileName);
            DirectoryOperations.CreateParentDirectory(destinationFilePath);

            if (File.Exists(destinationFilePath))
            {
                DebugLogger.LogFormat("Destination file {0} already exists, removing it.", destinationFilePath);
                FileOperations.Delete(destinationFilePath);
            }
            
            FileOperations.Move(sourceFilePath, destinationFilePath);
            _localMetaData.RegisterEntry(fileName, _versionId);
        }
    }
}