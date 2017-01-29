using System.IO;
using System.Linq;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class UninstallCommand : BaseAppUpdaterCommand, IUninstallCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(UninstallCommand));

        private readonly ILocalDirectory _localData;
        private readonly ILocalMetaData _localMetaData;

        private IGeneralStatusReporter _statusReporter;

        public UninstallCommand(ILocalDirectory localData, ILocalMetaData localMetaData)
        {
            AssertChecks.ArgumentNotNull(localData, "localData");
            AssertChecks.ArgumentNotNull(localMetaData, "localMetaData");

            DebugLogger.LogConstructor();

            _localData = localData;
            _localMetaData = localMetaData;
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");

            DebugLogger.Log("Preparing uninstallation.");

            _localData.PrepareForWriting();

            double weight = StatusWeightHelper.GetUninstallWeight();
            _statusReporter = statusMonitor.CreateGeneralStatusReporter(weight);
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            DebugLogger.Log("Uninstalling.");

            var entries = _localMetaData.GetRegisteredEntries();

            var files = entries.Where(s => !s.EndsWith("/"));
            var directories = entries.Where(s => s.EndsWith("/"));

            int counter = 0;

            foreach (var fileName in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var filePath = _localData.Path.PathCombine(fileName);

                if (File.Exists(filePath))
                {
                    FileOperations.Delete(filePath);
                }

                _localMetaData.UnregisterEntry(fileName);

                counter++;
                _statusReporter.OnProgressChanged(counter / (double)entries.Length);
            }

            foreach (var dirName in directories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dirPath = _localData.Path.PathCombine(dirName);

                if (Directory.Exists(dirPath) && DirectoryOperations.IsDirectoryEmpty(dirPath))
                {
                    DirectoryOperations.Delete(dirPath, false);
                }

                _localMetaData.UnregisterEntry(dirName);

                counter++;
                _statusReporter.OnProgressChanged(counter / (double)entries.Length);
            }
        }
    }
}