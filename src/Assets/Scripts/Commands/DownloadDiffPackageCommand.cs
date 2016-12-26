using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data.Remote;

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
            downloader.Download(cancellationToken);

            PackagePath = diffPath;
        }

        public string PackagePath { get; private set; }
    }
}