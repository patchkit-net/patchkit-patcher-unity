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

        public CheckVersionIntegrityCommand(int versionId, AppContentSummary versionSummary, ILocalDirectory localDirectory, ILocalMetaData localMetaData)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            // TODO: Validate the content summary.
            AssertChecks.ArgumentNotNull(localDirectory, "localDirectory");
            AssertChecks.ArgumentNotNull(localMetaData, "localMetaData");
            

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(versionId, "versionId");

            _versionId = versionId;
            _versionSummary = versionSummary;
            _localDirectory = localDirectory;
            _localMetaData = localMetaData;
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");

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

                _statusReporter.OnProgressChanged((i + 1)/(double)_versionSummary.Files.Length);
            }

            Results = new VersionIntegrity(files);
        }

        private FileIntegrity CheckFile(AppContentSummaryFile file)
        {
            if(!File.Exists(_localDirectory.Path.PathCombine(file.Path)))
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

            string hash = HashCalculator.ComputeFileHash(_localDirectory.Path.PathCombine(file.Path));

            if (hash != file.Hash)
            {
                return new FileIntegrity(file.Path, FileIntegrityStatus.InvalidHash);
            }

            // TODO: Check file size (always).

            return new FileIntegrity(file.Path, FileIntegrityStatus.Ok);
        }

        public VersionIntegrity Results { get; private set; }
    }
}