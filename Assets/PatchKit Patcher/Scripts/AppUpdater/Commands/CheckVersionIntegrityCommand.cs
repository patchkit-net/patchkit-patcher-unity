using System.IO;
using System.Linq;
using System.Diagnostics;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class CheckVersionIntegrityCommand : BaseAppUpdaterCommand, ICheckVersionIntegrityCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(CheckVersionIntegrityCommand));

        private readonly int _versionId;
        private readonly AppContentSummary _versionSummary;
        private readonly ILocalDirectory _localDirectory;
        private readonly ILocalMetaData _localMetaData;

        private OperationStatus _status;
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

        public override void Prepare(UpdaterStatus status)
        {
            base.Prepare(status);

            Checks.ArgumentNotNull(status, "statusMonitor");

            DebugLogger.Log("Preparing version integrity check.");

            _status = new OperationStatus
            {
                Weight = {Value = StatusWeightHelper.GetCheckVersionIntegrityWeight(_versionSummary)},
                Description = {Value = "Checking version integrity..."}
            };
            status.RegisterOperation(_status);
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            var integrityCheckStopwatch = new Stopwatch();
            var optionalParams = new PatcherStatistics.OptionalParams 
            {
                VersionId = _versionId,
            };

            System.Func<PatcherStatistics.OptionalParams> timedParams = () => new PatcherStatistics.OptionalParams {
                VersionId = optionalParams.VersionId,
                Time = integrityCheckStopwatch.Elapsed.Seconds,
            };

            try
            {
                PatcherStatistics.DispatchSendEvent("validation_started", optionalParams);
                ExecuteInternal(cancellationToken);
                
                if (Results.Files.All(integrity => integrity.Status == FileIntegrityStatus.Ok))
                {
                    PatcherStatistics.DispatchSendEvent("validation_succeeded", timedParams());
                }
                else
                {
                    PatcherStatistics.DispatchSendEvent("validation_failed", timedParams());
                }
            }
            catch (System.OperationCanceledException)
            {
                PatcherStatistics.DispatchSendEvent("validation_canceled", timedParams());
                throw;
            }
        }

        private void ExecuteInternal(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Checking version integrity.");

            _status.IsActive.Value = true;

            var files = new FileIntegrity[_versionSummary.Files.Length];

            for (int i = 0; i < _versionSummary.Files.Length; i++)
            {
                files[i] = CheckFile(_versionSummary.Files[i]);

                _status.Progress.Value = (i + 1) / (double) _versionSummary.Files.Length;
            }

            Results = new VersionIntegrity(files);

            _status.IsActive.Value = false;
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

            int actualVersionId = _localMetaData.GetEntryVersionId(file.Path);
            if (actualVersionId != _versionId)
            {
                return FileIntegrity.InvalidVersion(_versionId, actualVersionId, file.Path);
            }

            if (_isCheckingSize)
            {
                long actualSize = new FileInfo(localPath).Length;
                if (actualSize != file.Size)
                {
                    return FileIntegrity.InvalidSize(file.Size, actualSize, file.Path);
                }
            }

            if (_isCheckingHash)
            {
                string actualFileHash = HashCalculator.ComputeFileHash(localPath);
                if (actualFileHash != file.Hash)
                {
                    return FileIntegrity.InvalidHash(file.Hash, actualFileHash, file.Path);
                }
            }

            return new FileIntegrity(file.Path, FileIntegrityStatus.Ok);
        }

        public VersionIntegrity Results { get; private set; }
    }
}