using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class DownloadDiffPackageCommand : BaseAppUpdaterCommand, IDownloadDiffPackageCommand
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(DownloadDiffPackageCommand));

        private readonly int _versionId;
        private readonly AppUpdaterContext _context;

        private string _keySecret;
        private RemoteResource _resource;
        private IDownloadStatusReporter _statusReporter;

        public DownloadDiffPackageCommand(int versionId, AppUpdaterContext context)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            AssertChecks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(versionId, "versionId");

            _versionId = versionId;
            _context = context;
        }

        public override void Execute(CancellationToken cancellationToken)
        {
            base.Execute(cancellationToken);

            DebugLogger.Log("Downloading diff package.");

            string diffPath = _context.App.DownloadData.GetDiffPackagePath(_versionId);

            var resource = _context.App.RemoteData.GetDiffPackageResource(_versionId, _keySecret);

            var downloader = new RemoteResourceDownloader(diffPath, resource, _context.Configuration.UseTorrents);

            downloader.DownloadProgressChanged += _statusReporter.OnDownloadProgressChanged;

            _statusReporter.OnDownloadStarted();

            downloader.Download(cancellationToken);

            _statusReporter.OnDownloadEnded();

            PackagePath = diffPath;
        }

        public override void Prepare(IStatusMonitor statusMonitor)
        {
            base.Prepare(statusMonitor);

            AssertChecks.ArgumentNotNull(statusMonitor, "statusMonitor");

            DebugLogger.Log("Preparing diff package download.");

            _context.App.LocalData.EnableWriteAccess();
            _context.App.DownloadData.EnableWriteAccess();

            _resource = _context.App.RemoteData.GetContentPackageResource(_versionId, _keySecret);

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