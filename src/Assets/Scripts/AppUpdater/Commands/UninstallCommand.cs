using System.IO;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class UninstallCommand : BaseAppUpdaterCommand, IUninstallCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(UninstallCommand));

        private readonly AppUpdaterContext _context;

        private IGeneralStatusReporter _statusReporter;

        public UninstallCommand(AppUpdaterContext context)
        {
            AssertChecks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _context = context;
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            DebugLogger.Log("Uninstalling.");

            var fileNames = _context.App.LocalData.MetaData.GetFileNames();

            for (int i = 0; i < fileNames.Length; i++)
            {
                var fileName = fileNames[i];
                _context.App.LocalData.DeleteFile(fileName);
                string directoryName = Path.GetDirectoryName(fileName);
                if (_context.App.LocalData.IsDirectoryEmpty(directoryName))
                {
                    _context.App.LocalData.DeleteDirectory(directoryName);
                }

                _context.App.LocalData.MetaData.RemoveFile(fileName);

                _statusReporter.OnProgressChanged((i + 1)/(double) fileNames.Length);
            }
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");

            DebugLogger.Log("Preparing uninstallation.");

            double weight = StatusWeightHelper.GetUninstallWeight();
            _statusReporter = statusMonitor.CreateGeneralStatusReporter(weight);
        }
    }
}