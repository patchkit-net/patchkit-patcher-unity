using System;
using System.IO;
using System.Threading;
using JetBrains.Annotations;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class RemoteResourceDownloader
    {
        public delegate IHttpDownloader CreateNewHttpDownloader([NotNull] string destinationFilePath,
            [NotNull] string[] mirrorUrls, long size);

        public delegate IChunkedHttpDownloader CreateNewChunkedHttpDownloader(string destinationFilePath,
            RemoteResource resource, int timeout);

        public delegate ITorrentDownloader CreateNewTorrentDownloader(string destinationFilePath,
            RemoteResource resource, int timeout);

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(RemoteResourceDownloader));

        private const int TorrentDownloaderTimeout = 10000;
        private const int ChunkedHttpDownloaderTimeout = 30000;
        private const int RetriesCount = 8; // FIX: #722
        private readonly string _destinationFilePath;
        private readonly string _destinationMetaPath;

        private readonly RemoteResource _resource;

        private readonly bool _useTorrents;
        private readonly CreateNewHttpDownloader _createNewHttpDownloader;
        private readonly CreateNewChunkedHttpDownloader _createNewChunkedHttpDownloader;
        private readonly CreateNewTorrentDownloader _createNewTorrentDownloader;

        private bool _downloadHasBeenCalled;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public RemoteResourceDownloader(string destinationFilePath, string destinationMetaPath, RemoteResource resource,
            bool useTorrents) :
            this(destinationFilePath, destinationMetaPath, resource, useTorrents, CreateDefaultHttpDownloader,
                CreateDefaultChunkedHttpDownloader, CreateDefaultTorrentDownloader)
        {
        }

        public RemoteResourceDownloader(string destinationFilePath, string destinationMetaPath, RemoteResource resource,
            bool useTorrents,
            CreateNewHttpDownloader createNewHttpDownloader,
            CreateNewChunkedHttpDownloader createNewChunkedHttpDownloader,
            CreateNewTorrentDownloader createNewTorrentDownloader)
        {
            Checks.ArgumentParentDirectoryExists(destinationFilePath, "destinationFilePath");
            Checks.ArgumentValidRemoteResource(resource, "resource");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(destinationFilePath, "destinationFilePath");
            DebugLogger.LogVariable(resource, "resource");
            DebugLogger.LogVariable(useTorrents, "useTorrents");

            _destinationFilePath = destinationFilePath;
            _destinationMetaPath = destinationMetaPath;
            _resource = resource;
            _useTorrents = useTorrents;
            _createNewHttpDownloader = createNewHttpDownloader;
            _createNewChunkedHttpDownloader = createNewChunkedHttpDownloader;
            _createNewTorrentDownloader = createNewTorrentDownloader;
        }

        private bool TryDownloadWithTorrent(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Trying to download with torrent.");

            using (var downloader =
                _createNewTorrentDownloader(_destinationFilePath, _resource, TorrentDownloaderTimeout))
            {
                try
                {
                    downloader.DownloadProgressChanged += OnDownloadProgressChanged;

                    downloader.Download(cancellationToken);

                    return true;
                }
                catch (TorrentClientException exception)
                {
                    DebugLogger.LogException(exception);
                    return false;
                }
                catch (DownloaderException exception)
                {
                    DebugLogger.LogException(exception);
                    return false;
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
                _createNewChunkedHttpDownloader(_destinationFilePath, _resource, ChunkedHttpDownloaderTimeout))
            {
                downloader.DownloadProgressChanged += OnDownloadProgressChanged;

                downloader.Download(cancellationToken);
            }
        }

        private void DownloadWithHttp(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Downloading with HTTP.");

            var downloader = _createNewHttpDownloader(_destinationFilePath, _resource.GetUrls(), _resource.Size);
            downloader.DownloadProgressChanged += OnDownloadProgressChanged;
            downloader.Download(cancellationToken);
        }

        private bool AreChunksAvailable()
        {
            return _resource.ChunksData.ChunkSize > 0 && _resource.ChunksData.Chunks.Length > 0;
        }

        public void Download(CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _downloadHasBeenCalled, "Download");

            int retriesLeft = RetriesCount;
            do
            {
                try
                {
                    ResolveDownloader(cancellationToken);

                    var validator = new DownloadedResourceValidator();
                    validator.Validate(_destinationFilePath, _resource);

                    return;
                }
                catch (Exception ex)
                {
                    if (ex is OperationCanceledException ||
                        ex is UnauthorizedAccessException ||
                        ex is ThreadAbortException)
                    {
                        throw;
                    }

                    if (retriesLeft > 0)
                    {
                        retriesLeft--;
                        DebugLogger.LogWarningFormat("Resolving Dowloader failed, retry: {0}/{1}",
                            RetriesCount - retriesLeft + 1, RetriesCount);
                    }
                    else
                    {
                        DebugLogger.LogErrorFormat("Resolving Dowloader failed, no retries left, throwing further");
                        throw;
                    }
                }
            } while (retriesLeft > 0);
        }

        private void ResolveDownloader(CancellationToken cancellationToken)
        {
            if (_resource.HasMetaUrls())
            {
                DebugLogger.Log("Downloading meta data...");

                if (File.Exists(_destinationMetaPath))
                {
                    DebugLogger.Log("Removing previous meta data file: " + _destinationMetaPath);
                    File.Delete(_destinationMetaPath);
                }

                var httpDownloader = CreateDefaultHttpDownloader(_destinationMetaPath, _resource.GetMetaUrls(), 0);

                httpDownloader.Download(cancellationToken);

                DebugLogger.Log("Meta data downloaded");
            }

            DebugLogger.Log("Downloading content file");

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
                DebugLogger.Log("Chunsk data is available.");
                DownloadWithChunkedHttp(cancellationToken);
            }
            else
            {
                DebugLogger.Log("Chunks data is not available.");
                DownloadWithHttp(cancellationToken);
            }
        }

        protected virtual void OnDownloadProgressChanged(long downloadedBytes)
        {
            if (DownloadProgressChanged != null) DownloadProgressChanged(downloadedBytes);
        }

        private static IHttpDownloader CreateDefaultHttpDownloader(string destinationFilePath, string[] mirrorUrls,
            long size)
        {
            return new HttpDownloader(destinationFilePath, mirrorUrls, size);
        }

        private static IChunkedHttpDownloader CreateDefaultChunkedHttpDownloader(string destinationFilePath,
            RemoteResource resource,
            int timeout)
        {
            return new ChunkedHttpDownloader(destinationFilePath, resource, timeout);
        }

        private static ITorrentDownloader CreateDefaultTorrentDownloader(string destinationFilePath,
            RemoteResource resource,
            int timeout)
        {
            return new TorrentDownloader(destinationFilePath, resource, timeout);
        }
    }
}