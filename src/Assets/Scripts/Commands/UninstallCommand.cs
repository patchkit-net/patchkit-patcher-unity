using System.IO;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.Commands
{
    internal class UninstallCommand : IUninstallCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(UninstallCommand));

        private readonly PatcherContext _context;

        private IGeneralStatusReporter _statusReporter;

        public UninstallCommand(PatcherContext context)
        {
            DebugLogger.LogConstructor();

            Assert.IsNotNull(context, "context");

            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Uninstalling.");

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

                _statusReporter.OnProgressChanged((i + 1)/(double) fileNames.Length);
            }
        }

        public void Prepare(IStatusMonitor statusMonitor)
        {
            DebugLogger.Log("Preparing uninstallation.");

            double weight = StatusWeightHelper.GetUninstallWeight();
            _statusReporter = statusMonitor.CreateGeneralStatusReporter(weight);
        }
    }
}