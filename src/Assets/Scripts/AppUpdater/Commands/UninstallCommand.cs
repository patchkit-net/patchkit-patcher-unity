using System.IO;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class UninstallCommand : BaseAppUpdaterCommand, IUninstallCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(UninstallCommand));

        private readonly ILocalData _localData;
        private readonly ILocalMetaData _localMetaData;

        private IGeneralStatusReporter _statusReporter;

        public UninstallCommand(ILocalData localData, ILocalMetaData localMetaData)
        {
            AssertChecks.ArgumentNotNull(localData, "localData");
            AssertChecks.ArgumentNotNull(localMetaData, "localMetaData");

            DebugLogger.LogConstructor();

            _localData = localData;
            _localMetaData = localMetaData;
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            DebugLogger.Log("Uninstalling.");

            var fileNames = _localMetaData.GetFileNames();

            for (int i = 0; i < fileNames.Length; i++)
            {
                var fileName = fileNames[i];
                _localData.DeleteFile(fileName);
                string directoryName = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrEmpty(directoryName) && 
                    _localData.DirectoryExists(directoryName) && 
                    _localData.IsDirectoryEmpty(directoryName))
                {
                    _localData.DeleteDirectory(directoryName);
                }

                _localMetaData.RemoveFile(fileName);

                _statusReporter.OnProgressChanged((i + 1)/(double) fileNames.Length);
            }
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");

            DebugLogger.Log("Preparing uninstallation.");

            _localData.EnableWriteAccess();

            double weight = StatusWeightHelper.GetUninstallWeight();
            _statusReporter = statusMonitor.CreateGeneralStatusReporter(weight);
        }
    }
}