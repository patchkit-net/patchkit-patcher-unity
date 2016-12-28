using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data.Remote;
using PatchKit.Unity.Patcher.Progress;

namespace PatchKit.Unity.Patcher.Commands
{
    internal class DownloadContentPackageCommand : IDownloadContentPackageCommand
    {
        private readonly int _versionId;
        private readonly string _keySecret;
        private readonly PatcherContext _context;

        public DownloadContentPackageCommand(int versionId, string keySecret, PatcherContext context)
        {
            _versionId = versionId;
            _keySecret = keySecret;
            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            string contentPath = _context.Data.LocalData.DownloadData.GetContentPackagePath(_versionId);

            var resource = _context.Data.RemoteData.GetContentPackageResource(_versionId, _keySecret);

            var downloader = new RemoteResourceDownloader(contentPath, resource, _context.Configuration.UseTorrents);

            LinkDownloaderProgressReporter(downloader, resource);

            downloader.Download(cancellationToken);

            PackagePath = contentPath;
        }

        public void Prepare(IProgressMonitor progressMonitor)
        {
            throw new System.NotImplementedException();
        }

        private void LinkDownloaderProgressReporter(RemoteResourceDownloader downloader, RemoteResource resource)
        {
            var progressWeight = ProgressWeightHelper.GetResourceDownloadWeight(resource.Size);
            var progressReporter = _context.ProgressMonitor.CreateDownloadProgressReporter(progressWeight);
            downloader.DownloadProgressChanged += progressReporter.OnDownloadProgressChanged;
        }

        public string PackagePath { get; private set; }
    }
}