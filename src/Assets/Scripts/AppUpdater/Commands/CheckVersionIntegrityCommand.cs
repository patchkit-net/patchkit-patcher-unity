using PatchKit.Api.Models;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class CheckVersionIntegrityCommand : ICheckVersionIntegrityCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(CheckVersionIntegrityCommand));

        private readonly int _versionId;
        private readonly AppUpdaterContext _context;

        private AppContentSummary _versionSummary;
        private IGeneralStatusReporter _statusReporter;

        public CheckVersionIntegrityCommand(int versionId, AppUpdaterContext context)
        {
            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(versionId, "versionId");

            Checks.ArgumentValidVersionId(versionId, "versionId");
            Assert.IsNotNull(context, "context");

            _versionId = versionId;
            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Checking version integrity.");

            var files = new FileIntegrity[_versionSummary.Files.Length];

            for (int i = 0; i < _versionSummary.Files.Length; i++)
            {
                files[i] = CheckFile(_versionSummary.Files[i]);

                _statusReporter.OnProgressChanged((i + 1)/(double)_versionSummary.Files.Length);
            }

            Results = new VersionIntegrity(files);
        }

        public void Prepare(IStatusMonitor statusMonitor)
        {
            DebugLogger.Log("Preparing version integrity check.");

            _versionSummary = _context.Data.RemoteData.MetaData.GetContentSummary(_versionId);

            double weight = StatusWeightHelper.GetCheckVersionIntegrityWeight(_versionSummary);
            _statusReporter = statusMonitor.CreateGeneralStatusReporter(weight);
        }

        private FileIntegrity CheckFile(AppContentSummaryFile file)
        {
            if (!_context.Data.LocalData.FileExists(file.Path))
            {
                return new FileIntegrity(file.Path, FileIntegrityStatus.MissingData);
            }

            if (!_context.Data.LocalData.MetaData.FileExists(file.Path))
            {
                return new FileIntegrity(file.Path, FileIntegrityStatus.MissingMetaData);
            }

            if (_context.Data.LocalData.MetaData.GetFileVersion(file.Path) != _versionId)
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