using System.IO;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class CheckVersionIntegrityCommand : BaseAppUpdaterCommand, ICheckVersionIntegrityCommand
    {

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(CheckVersionIntegrityCommand));

        private readonly int _versionId;
        private readonly AppContentSummary _versionSummary;
        private readonly ILocalDirectory _localDirectory;
        private readonly ILocalMetaData _localMetaData;

        private IGeneralStatusReporter _statusReporter;
        bool _isCheckingHash;
        bool _isCheckingSize;

        public CheckVersionIntegrityCommand(int versionId, AppContentSummary versionSummary,
            ILocalDirectory localDirectory, ILocalMetaData localMetaData, bool isCheckingHash, bool isCheckingSize)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            Checks.ArgumentNotNull(versionSummary, "versionSummary");
            Checks.ArgumentNotNull(localDirectory, "localDirectory");
            Checks.ArgumentNotNull(localMetaData, "localMetaData");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(versionId, "versionId");

            _versionId = versionId;
            _versionSummary = versionSummary;
            _localDirectory = localDirectory;
            _localMetaData = localMetaData;
            _isCheckingSize = isCheckingSize;
            _isCheckingHash = isCheckingHash;
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            Checks.ArgumentNotNull(statusMonitor, "statusMonitor");

            DebugLogger.Log("Preparing version integrity check.");

            double weight = StatusWeightHelper.GetCheckVersionIntegrityWeight(_versionSummary);
            _statusReporter = statusMonitor.CreateGeneralStatusReporter(weight);
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            DebugLogger.Log("Checking version integrity.");

            var files = new FileIntegrity[_versionSummary.Files.Length];

            for (int i = 0; i < _versionSummary.Files.Length; i++)
            {
                files[i] = CheckFile(_versionSummary.Files[i]);

                _statusReporter.OnProgressChanged((i + 1)/(double)_versionSummary.Files.Length, "Checking version integrity...");
            }

            Results = new VersionIntegrity(files);
        }

        private FileIntegrity CheckFile(AppContentSummaryFile file)
        {
            string localPath = _localDirectory.Path.PathCombine(file.Path);
            if (!File.Exists(localPath))
            {
                return new FileIntegrity(file.Path, FileIntegrityStatus.MissingData);
            }

            if (!_localMetaData.IsEntryRegistered(file.Path))
            {
                return new FileIntegrity(file.Path, FileIntegrityStatus.MissingMetaData);
            }

            if (_localMetaData.GetEntryVersionId(file.Path) != _versionId)
            {
                return new FileIntegrity(file.Path, FileIntegrityStatus.InvalidVersion);
            }

            if (_isCheckingSize)
            {
                long size = new FileInfo(localPath).Length;
                if (size != file.Size)
                {
                    return new FileIntegrity(file.Path, FileIntegrityStatus.InvalidSize);
                }
            }

            if (_isCheckingHash)
            {
                string hash = HashCalculator.ComputeFileHash(localPath);
                if (hash != file.Hash)
                {
                    return new FileIntegrity(file.Path, FileIntegrityStatus.InvalidHash);
                }
            }

            return new FileIntegrity(file.Path, FileIntegrityStatus.Ok);
        }

        public VersionIntegrity Results { get; private set; }
    }
}