using System;
using System.IO;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using UniRx;
using CancellationToken = PatchKit.Unity.Patcher.Cancellation.CancellationToken;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class DownloadPackageCommand : BaseAppUpdaterCommand, IDownloadPackageCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(DownloadPackageCommand));

        private readonly RemoteResource _resource;
        private readonly string _destinationPackagePath;
        private readonly string _destinationMetaPath;

        private DownloadStatus _status;

        public DownloadPackageCommand(RemoteResource resource, string destinationPackagePath,
            string destinationMetaPath)
        {
            Checks.ArgumentValidRemoteResource(resource, "resource");

            if (string.IsNullOrEmpty(destinationPackagePath))
            {
                throw new ArgumentException(destinationPackagePath, "destinationPackagePath");
            }

            if (!Directory.Exists(Path.GetDirectoryName(destinationPackagePath)))
            {
                throw new ArgumentException("Parent directory doesn't exist.", "destinationPackagePath");
            }

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(resource, "resource");
            DebugLogger.LogVariable(destinationPackagePath, "destinationPackagePath");

            _resource = resource;
            _destinationPackagePath = destinationPackagePath;
            _destinationMetaPath = destinationMetaPath;
        }

        public override void Prepare(UpdaterStatus status, CancellationToken cancellationToken)
        {
            base.Prepare(status, cancellationToken);

            if (status == null)
            {
                throw new ArgumentNullException("status");
            }

            DebugLogger.Log("Preparing package download.");

            _status = new DownloadStatus
            {
                Weight = {Value = StatusWeightHelper.GetResourceDownloadWeight(_resource)},
                Description = {Value = "Downloading package..."}
            };
            status.RegisterOperation(_status);
        }

        public override void Execute(Cancellation.CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            DebugLogger.Log("Downloading package.");

            _status.IsActive.Value = true;
            _status.TotalBytes.Value = _resource.Size;

            var downloader = new RemoteResourceDownloader(_destinationPackagePath, _destinationMetaPath, _resource);

            downloader.DownloadProgressChanged += bytes => { _status.Bytes.Value = bytes; };

            var downloadStartTime = DateTime.Now;

            var stalledTimeout = TimeSpan.FromSeconds(10);
            
            using (_status.BytesPerSecond.Subscribe(bps =>
                _status.Description.Value = bps > 0.01 || DateTime.Now - downloadStartTime < stalledTimeout ? "Downloading package..." : "Stalled..."))
            {
                downloader.Download(cancellationToken);
            }

            _status.IsActive.Value = false;
        }
    }
}
