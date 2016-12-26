using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data.Remote;

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
            downloader.Download(cancellationToken);

            PackagePath = contentPath;
        }

        public string PackagePath { get; private set; }
    }
}