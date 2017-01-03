using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data.Remote;
using PatchKit.Unity.Patcher.Progress;

namespace PatchKit.Unity.Patcher.Commands
{
    internal class DownloadDiffPackageCommand : IDownloadDiffPackageCommand
    {
        private readonly int _versionId;
        private readonly string _keySecret;
        private readonly PatcherContext _context;

        public DownloadDiffPackageCommand(int versionId, string keySecret, PatcherContext context)
        {
            _versionId = versionId;
            _keySecret = keySecret;
            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            string diffPath = _context.Data.LocalData.DownloadData.GetDiffPackagePath(_versionId);

            var resource = _context.Data.RemoteData.GetDiffPackageResource(_versionId, _keySecret);

            var downloader = new RemoteResourceDownloader(diffPath, resource, _context.Configuration.UseTorrents);

            LinkDownloaderProgressReporter(downloader, resource);

            downloader.Download(cancellationToken);

            PackagePath = diffPath;
        }

        public void Prepare(IStatusMonitor statusMonitor)
        {
            throw new System.NotImplementedException();
        }

        private void LinkDownloaderProgressReporter(RemoteResourceDownloader downloader, RemoteResource resource)
        {
            var progressWeight = ProgressWeightHelper.GetResourceDownloadWeight(resource.Size);
            var progressReporter = _context.StatusMonitor.CreateDownloadProgressReporter(progressWeight);
            downloader.DownloadProgressChanged += progressReporter.OnDownloadProgressChanged;
        }

        public string PackagePath { get; private set; }
    }
}