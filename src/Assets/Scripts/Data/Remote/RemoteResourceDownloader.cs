using System;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data.Remote.Downloaders;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.Data.Remote
{
    internal class RemoteResourceDownloader
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(RemoteResourceDownloader));

        private const int TorrentDownloaderTimeout = 10000;
        private const int ChunkedHttpDownloaderTimeout = 10000;
        private const int HttpDownloaderTimeout = 10000;

        private readonly string _destinationFilePath;

        private readonly RemoteResource _resource;

        private readonly bool _useTorrents;

        private bool _started;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public RemoteResourceDownloader(string destinationFilePath, RemoteResource resource, bool useTorrents)
        {
            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(destinationFilePath, "destinationFilePath");
            DebugLogger.LogVariable(useTorrents, "useTorrents");

            Checks.ArgumentDirectoryOfFileExists(destinationFilePath, "destinationFilePath");
            Checks.ArgumentValidRemoteResource(resource, "resource");

            _destinationFilePath = destinationFilePath;
            _resource = resource;
            _useTorrents = useTorrents;
        }

        private bool TryDownloadWithTorrent(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Trying to download with torrent.");

            using (var downloader = 
                new TorrentDownloader(_destinationFilePath, _resource, TorrentDownloaderTimeout))
            {
                try
                {
                    downloader.Download(cancellationToken);

                    return true;
                }
                catch (Exception exception)
                {
                    DebugLogger.LogException(exception);
                    return false;
                }
            }
        }

        private void DownloadWithChunkedHttp(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Downloading with chunked HTTP.");

            using (var downloader =
                new ChunkedHttpDownloader(_destinationFilePath, _resource, ChunkedHttpDownloaderTimeout))
            {
                downloader.Download(cancellationToken);
            }
        }

        private void DownloadWithHttp(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Downloading with HTTP.");

            using (var downloader =
                new HttpDownloader(_destinationFilePath, _resource, HttpDownloaderTimeout))
            {
                downloader.Download(cancellationToken);
            }
        }

        private bool AreChunksAvailable()
        {
            return _resource.ChunksData.ChunkSize > 0 && _resource.ChunksData.Chunks.Length > 0;
        }

        public void Download(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Starting download.");

            if (_started)
            {
                throw new InvalidOperationException("Cannot start the same RemoteResourceDownloader twice.");
            }
            _started = true;

            if (_useTorrents)
            {
                bool downloaded = TryDownloadWithTorrent(cancellationToken);

                if (downloaded)
                {
                    return;
                }
            }

            if (AreChunksAvailable())
            {
                DownloadWithChunkedHttp(cancellationToken);
            }
            else
            {
                DownloadWithHttp(cancellationToken);
            }
        }

        protected virtual void OnDownloadProgressChanged(long downloadedBytes, long totalBytes)
        {
            if (DownloadProgressChanged != null) DownloadProgressChanged(downloadedBytes, totalBytes);
        }
    }
}