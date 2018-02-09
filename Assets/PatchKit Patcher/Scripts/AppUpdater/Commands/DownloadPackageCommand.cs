using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppUpdater.Status;
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

        private DownloadStatus _status;

        public DownloadPackageCommand(RemoteResource resource, string destinationPackagePath,
            string destinationMetaPath, bool useTorrents)
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

        public override void Prepare(UpdaterStatus status)
        {
            base.Prepare(status);

            Checks.ArgumentNotNull(status, "statusMonitor");

            DebugLogger.Log("Preparing package download.");

            _status = new DownloadStatus
            {
                Weight = {Value = StatusWeightHelper.GetResourceDownloadWeight(_resource)},
                Description = {Value = "Downloading package..."}
            };
            status.RegisterOperation(_status);
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            DebugLogger.Log("Downloading package.");

            _status.IsActive.Value = true;
            _status.TotalBytes.Value = _resource.Size;

            var downloader = new RemoteResourceDownloader(_destinationPackagePath, _destinationMetaPath, _resource,
                _useTorrents);

            downloader.DownloadProgressChanged += bytes => { _status.Bytes.Value = bytes; };

            downloader.Download(cancellationToken);

            _status.IsActive.Value = false;
        }
    }
}