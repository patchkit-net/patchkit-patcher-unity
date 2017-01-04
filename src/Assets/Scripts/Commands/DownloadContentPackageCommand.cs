using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data.Remote;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.Commands
{
    internal class DownloadContentPackageCommand : IDownloadContentPackageCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(DownloadContentPackageCommand));

        private readonly int _versionId;
        private readonly string _keySecret;
        private readonly PatcherContext _context;

        private RemoteResource _resource;
        private IDownloadStatusReporter _statusReporter;

        public DownloadContentPackageCommand(int versionId, string keySecret, PatcherContext context)
        {
            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(versionId, "versionId");
            DebugLogger.LogVariable(keySecret, "keySecret");

            Checks.ArgumentValidVersionId(versionId, "versionId");
            Checks.ArgumentNotNullOrEmpty(keySecret, "keySecret");
            Assert.IsNotNull(context, "context");

            _versionId = versionId;
            _keySecret = keySecret;
            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Downloading content package.");

            string contentPath = _context.Data.LocalData.DownloadData.GetContentPackagePath(_versionId);

            var downloader = new RemoteResourceDownloader(contentPath, _resource, _context.Configuration.UseTorrents);

            downloader.DownloadProgressChanged += _statusReporter.OnDownloadProgressChanged;

            _statusReporter.OnDownloadStarted();

            downloader.Download(cancellationToken);

            _statusReporter.OnDownloadEnded();

            PackagePath = contentPath;
        }

        public void Prepare(IStatusMonitor statusMonitor)
        {
            DebugLogger.Log("Preparing content package download.");

            _resource = _context.Data.RemoteData.GetContentPackageResource(_versionId, _keySecret);

            double weight = StatusWeightHelper.GetResourceDownloadWeight(_resource);
            _statusReporter = statusMonitor.CreateDownloadStatusReporter(weight);
        }

        public string PackagePath { get; private set; }
    }
}