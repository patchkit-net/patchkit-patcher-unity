using PatchKit.Api.Models;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Progress;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.Commands
{
    internal class CheckVersionIntegrityCommand : ICheckVersionIntegrityCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(CheckVersionIntegrityCommand));

        private readonly int _versionId;
        private readonly PatcherContext _context;

        public CheckVersionIntegrityCommand(int versionId, PatcherContext context)
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

            var summary = _context.Data.RemoteData.MetaData.GetContentSummary(_versionId);

            var progressWeight = ProgressWeightHelper.GetCheckVersionIntegrityWeight(summary.Size);
            var progressReporter = _context.ProgressMonitor.AddGeneralProgress(progressWeight);

            var results = new FileIntegrity[summary.Files.Length];

            for (int i = 0; i < summary.Files.Length; i++)
            {
                results[i] = CheckFile(summary.Files[i]);

                progressReporter.OnProgressChanged((i + 1)/(double) summary.Files.Length);
            }

            Results = results;
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

        public FileIntegrity[] Results { get; private set; }
    }
}