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
            Checks.ArgumentNotNull(localData, "localData");
            Checks.ArgumentNotNull(localMetaData, "localMetaData");

            DebugLogger.LogConstructor();

            _localData = localData;
            _localMetaData = localMetaData;
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            Checks.ArgumentNotNull(statusMonitor, "statusMonitor");

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

            var files = entries.Where(s => !s.EndsWith("/")).ToArray();
            // TODO: Uncomment this after fixing directory registration in install content command
            //var directories = entries.Where(s => s.EndsWith("/"));

            int counter = 0;

            _statusReporter.OnProgressChanged(0.0, "Uninstalling...");
            
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
                _statusReporter.OnProgressChanged(counter / (double)entries.Length, "Uninstalling...");
            }

            // TODO: Delete this after fixing directory registration in install content command
            // Temporary solution for deleting directories during uninstallation.
            foreach (var fileName in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string parentDirName = fileName;

                do
                {
                    parentDirName = Path.GetDirectoryName(parentDirName);

                    var parentDirPath = _localData.Path.PathCombine(parentDirName);

                    if (Directory.Exists(parentDirPath))
                    {
                        if (DirectoryOperations.IsDirectoryEmpty(parentDirPath))
                        {
                            DirectoryOperations.Delete(parentDirPath, false);
                        }
                        else
                        {
                            break;
                        }
                    }
                } while (parentDirName != null);
            }

            // TODO: Uncomment this after fixing directory registration in install content command
            /*
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
            }*/
            
            _statusReporter.OnProgressChanged(1.0, "Uninstalling...");
        }
    }
}