using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class CheckVersionIntegrityCommand : BaseAppUpdaterCommand, ICheckVersionIntegrityCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(CheckVersionIntegrityCommand));

        private readonly int _versionId;
        private readonly AppUpdaterContext _context;

        private AppContentSummary _versionSummary;
        private IGeneralStatusReporter _statusReporter;

        public CheckVersionIntegrityCommand(int versionId, AppUpdaterContext context)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            AssertChecks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(versionId, "versionId");

            _versionId = versionId;
            _context = context;
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

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");

            DebugLogger.Log("Preparing version integrity check.");

            _versionSummary = _context.App.RemoteMetaData.GetContentSummary(_versionId);

            double weight = StatusWeightHelper.GetCheckVersionIntegrityWeight(_versionSummary);
            _statusReporter = statusMonitor.CreateGeneralStatusReporter(weight);
        }

        private FileIntegrity CheckFile(AppContentSummaryFile file)
        {
            if (!_context.App.LocalData.FileExists(file.Path))
            {
                return new FileIntegrity(file.Path, FileIntegrityStatus.MissingData);
            }

            if (!_context.App.LocalMetaData.FileExists(file.Path))
            {
                return new FileIntegrity(file.Path, FileIntegrityStatus.MissingMetaData);
            }

            if (_context.App.LocalMetaData.GetFileVersionId(file.Path) != _versionId)
            {
                return new FileIntegrity(file.Path, FileIntegrityStatus.InvalidVersion);
            }

            // TODO: Check file size (always).
            // TODO: Check file hash (only if enabled).

            return new FileIntegrity(file.Path, FileIntegrityStatus.Ok);
        }

        public VersionIntegrity Results { get; private set; }
    }
}