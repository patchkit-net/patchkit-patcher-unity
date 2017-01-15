using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class DownloadDiffPackageCommand : IDownloadDiffPackageCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(DownloadDiffPackageCommand));

        private readonly int _versionId;
        private readonly AppUpdaterContext _context;

        private string _keySecret;
        private RemoteResource _resource;
        private IDownloadStatusReporter _statusReporter;

        public DownloadDiffPackageCommand(int versionId, AppUpdaterContext context)
        {
            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(versionId, "versionId");

            Checks.ArgumentValidVersionId(versionId, "versionId");
            Assert.IsNotNull(context, "context");

            _versionId = versionId;
            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Downloading diff package.");

            string diffPath = _context.Data.LocalData.DownloadData.GetDiffPackagePath(_versionId);

            var resource = _context.Data.RemoteData.GetDiffPackageResource(_versionId, _keySecret);

            var downloader = new RemoteResourceDownloader(diffPath, resource, _context.Configuration.UseTorrents);

            downloader.DownloadProgressChanged += _statusReporter.OnDownloadProgressChanged;

            _statusReporter.OnDownloadStarted();

            downloader.Download(cancellationToken);

            _statusReporter.OnDownloadEnded();

            PackagePath = diffPath;
        }

        public void Prepare(IStatusMonitor statusMonitor)
        {
            DebugLogger.Log("Preparing diff package download.");

            _resource = _context.Data.RemoteData.GetContentPackageResource(_versionId, _keySecret);

            double weight = StatusWeightHelper.GetResourceDownloadWeight(_resource);
            _statusReporter = statusMonitor.CreateDownloadStatusReporter(weight);
        }

        public void SetKeySecret(string keySecret)
        {
            _keySecret = keySecret;
        }

        public string PackagePath { get; private set; }
    }
}