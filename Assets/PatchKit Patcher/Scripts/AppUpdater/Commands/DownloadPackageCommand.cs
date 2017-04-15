using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class DownloadPackageCommand : BaseAppUpdaterCommand, IDownloadPackageCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(DownloadPackageCommand));

        private readonly RemoteResource _resource;
        private readonly string _destinationPackagePath;
        private readonly string _destinationMetaPath;
        private readonly bool _useTorrents;

        private IDownloadStatusReporter _statusReporter;

        public DownloadPackageCommand(RemoteResource resource, string destinationPackagePath, string destinationMetaPath, bool useTorrents)
        {
            Checks.ArgumentValidRemoteResource(resource, "resource");
            Checks.ArgumentNotNullOrEmpty(destinationPackagePath, "destinationPackagePath");
            Checks.ParentDirectoryExists(destinationPackagePath);

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(resource, "resource");
            DebugLogger.LogVariable(destinationPackagePath, "destinationPackagePath");
            DebugLogger.LogVariable(useTorrents, "useTorrents");

            _resource = resource;
            _destinationPackagePath = destinationPackagePath;
            _destinationMetaPath = destinationMetaPath;
            _useTorrents = useTorrents;
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            Checks.ArgumentNotNull(statusMonitor, "statusMonitor");

            DebugLogger.Log("Preparing package download.");

            double weight = StatusWeightHelper.GetResourceDownloadWeight(_resource);
            _statusReporter = statusMonitor.CreateDownloadStatusReporter(weight);
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            DebugLogger.Log("Downloading package.");

            var downloader = new RemoteResourceDownloader(_destinationPackagePath, _destinationMetaPath, _resource,
                _useTorrents);

            downloader.DownloadProgressChanged += _statusReporter.OnDownloadProgressChanged;

            _statusReporter.OnDownloadStarted();

            downloader.Download(cancellationToken);

            _statusReporter.OnDownloadEnded();
        }
    }
}
