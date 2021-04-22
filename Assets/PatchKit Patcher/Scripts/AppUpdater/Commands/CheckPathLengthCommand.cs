using System.IO;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;


namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class CheckPathLengthCommand : ICheckPathLengthCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(CheckPathLengthCommand));

        private readonly AppContentSummary? _contentSummary;
        private readonly AppDiffSummary? _diffSummary;
        private readonly string _localDirectoryPath;
        private OperationStatus _status;

        public CheckPathLengthCommand(AppContentSummary contentSummary, string localDirectoryPath)
        {
            Checks.ArgumentNotNull(localDirectoryPath, "localDirectoryPath");

            _contentSummary = contentSummary;
            _localDirectoryPath = localDirectoryPath;
        }

        public CheckPathLengthCommand(AppDiffSummary diffSummary, string localDirectoryPath)
        {
            Checks.ArgumentNotNull(localDirectoryPath, "localDirectoryPath");

            _diffSummary = diffSummary;
            _localDirectoryPath = localDirectoryPath;
        }
   
        public void Execute(CancellationToken cancellationToken)
        {
            _status.IsActive.Value = true;
            _status.IsIdle.Value = true;

            try
            {
                string pathFile;
                if (UseContentSummary())
                {
                    foreach (AppContentSummaryFile contentSummaryFile in _contentSummary.Value.Files)
                    {
                        pathFile = Path.Combine(_localDirectoryPath, contentSummaryFile.Path);
                        
                        if (pathFile.Length > 259)
                        {
                            throw new FilePathTooLongException(string.Format(
                                "Cannot install file {0}, the destination path length has exceeded Windows path length limit (260).",
                                pathFile));
                        }
                    }
                }
                else
                {
                    foreach (string contentSummaryFile in _diffSummary.Value.AddedFiles)
                    {
                        pathFile = Path.Combine(_localDirectoryPath, contentSummaryFile);
                        if (pathFile.Length > 259)
                        {
                            throw new FilePathTooLongException(string.Format(
                                "Cannot install file {0}, the destination path length has exceeded Windows path length limit (260).",
                                pathFile));
                        }
                    }
                }
            }
            finally
            {
                _status.IsActive.Value = false;
            }
        }
        
        private bool UseContentSummary()
        {
            return _contentSummary != null;
        }
        
        public void Prepare(UpdaterStatus status, CancellationToken cancellationToken)
        {
            _status = new OperationStatus
            {
                Weight = {Value = 0.00001},
                Description = {Value = "Check path length..."}
            };
            status.RegisterOperation(_status);
        }
    }
}
