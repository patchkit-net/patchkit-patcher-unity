using System.IO;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Progress;

namespace PatchKit.Unity.Patcher.Commands
{
    internal class UninstallCommand : IUninstallCommand
    {
        private readonly PatcherContext _context;

        public UninstallCommand(PatcherContext context)
        {
            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            // TODO: Calculate size of removed files.
            var progressWeight = ProgressWeightHelper.GetRemoveFilesWeight(1);
            var progressReporter = _context.StatusMonitor.CreateGeneralProgressReporter(progressWeight);

            var fileNames = _context.Data.LocalData.MetaData.GetFileNames();

            for (int i = 0; i < fileNames.Length; i++)
            {
                var fileName = fileNames[i];
                _context.Data.LocalData.DeleteFile(fileName);
                string directoryName = Path.GetDirectoryName(fileName);
                if (_context.Data.LocalData.IsDirectoryEmpty(directoryName))
                {
                    _context.Data.LocalData.DeleteDirectory(directoryName);
                }

                _context.Data.LocalData.MetaData.RemoveFile(fileName);

                progressReporter.OnProgressChanged((i + 1)/(double) fileNames.Length);
            }
        }

        public void Prepare(IStatusMonitor statusMonitor)
        {
            throw new System.NotImplementedException();
        }
    }
}