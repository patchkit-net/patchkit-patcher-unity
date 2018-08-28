using System;
using JetBrains.Annotations;
using PatchKit.Api.Models.Main;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using ILogger = PatchKit.Logging.ILogger;
using BytesRange = PatchKit.Network.BytesRange;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class RemoteResourceDownloader
    {
        public delegate IHttpDownloader CreateNewHttpDownloader([NotNull] string destinationFilePath,
            [NotNull] string[] urls);

        public delegate IChunkedHttpDownloader CreateNewChunkedHttpDownloader([NotNull] string destinationFilePath,
            [NotNull] ResourceUrl[] urls, ChunksData chunksData,
            long size);

        public delegate ITorrentDownloader CreateNewTorrentDownloader([NotNull] string destinationFilePath,
            [NotNull] string torrentFilePath,
            long totalBytes);


        private readonly ILogger _logger;

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

        public RemoteResourceDownloader([NotNull] string destinationFilePath, [NotNull] string destinationMetaPath,
            RemoteResource resource,
            bool useTorrents,
            CreateNewHttpDownloader createNewHttpDownloader,
            CreateNewChunkedHttpDownloader createNewChunkedHttpDownloader,
            CreateNewTorrentDownloader createNewTorrentDownloader)
        {
            if (destinationFilePath == null) throw new ArgumentNullException("destinationFilePath");
            if (destinationMetaPath == null) throw new ArgumentNullException("destinationMetaPath");

            _logger = PatcherLogManager.DefaultLogger;
            _destinationFilePath = destinationFilePath;
            _destinationMetaPath = destinationMetaPath;
            _resource = resource;
            _useTorrents = useTorrents;
            _createNewHttpDownloader = createNewHttpDownloader;
            _createNewChunkedHttpDownloader = createNewChunkedHttpDownloader;
            _createNewTorrentDownloader = createNewTorrentDownloader;
        }

        private string TorrentFilePath
        {
            get { return _destinationFilePath + ".torrent"; }
        }

        private void DownloadMeta(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Downloading resource meta...");

            var downloader = _createNewHttpDownloader(_destinationMetaPath, _resource.GetMetaUrls());
            downloader.Download(cancellationToken);

            _logger.LogDebug("Resource meta downloaded.");
        }

        private void DownloadTorrentFile(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Downloading torrent file...");
            _logger.LogTrace("torrentFilePath = " + TorrentFilePath);

            var torrentFileDownloader = _createNewHttpDownloader(TorrentFilePath, _resource.TorrentUrls);
            torrentFileDownloader.Download(cancellationToken);

            _logger.LogDebug("Torrent file downloaded.");
        }

        private void DownloadWithTorrents(CancellationToken cancellationToken)
        {
            DownloadTorrentFile(cancellationToken);

            _logger.LogDebug("Downloading resource with torrents...");

            var downloader = _createNewTorrentDownloader(_destinationFilePath, TorrentFilePath, _resource.Size);
            downloader.DownloadProgressChanged += OnDownloadProgressChanged;
            downloader.Download(cancellationToken);

            _logger.LogDebug("Resource has been downloaded with torrents.");
        }

        private void DownloadWithChunkedHttp(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Downloading resource with chunked HTTP...");

            var downloader = _createNewChunkedHttpDownloader(_destinationFilePath, _resource.ResourceUrls,
                _resource.ChunksData, _resource.Size);
            downloader.DownloadProgressChanged += OnDownloadProgressChanged;
            downloader.Download(cancellationToken);

            _logger.LogDebug("Resource has been downloaded with chunked HTTP.");
        }

        private void DownloadWithHttp(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Downloading resource with HTTP...");

            var downloader = _createNewHttpDownloader(_destinationFilePath, _resource.GetUrls());
            downloader.DownloadProgressChanged += OnDownloadProgressChanged;
            downloader.Download(cancellationToken);

            _logger.LogDebug("Resource has been downloaded with HTTP.");
        }

        private bool AreChunksAvailable()
        {
            return _resource.ChunksData.ChunkSize > 0 && _resource.ChunksData.Chunks.Length > 0;
        }

        private bool AreMetaAvailable()
        {
            return _resource.HasMetaUrls();
        }

        private bool ShouldUseTorrents()
        {
            if (!_useTorrents)
            {
                return false;
            }

            string forceHttpVal;
            bool isForceHttpSet = EnvironmentInfo.TryReadEnvironmentVariable(
                EnvironmentVariables.ForceHttp, out forceHttpVal);

            if (!isForceHttpSet)
            {
                return true;
            }

            bool forceHttp;
            if (Boolean.TryParse(forceHttpVal, out forceHttp))
            {
                return !forceHttp;
            }

            return true;
        }

        public void Download(CancellationToken cancellationToken)
        {
            try
            {
                Assert.MethodCalledOnlyOnce(ref _downloadHasBeenCalled, "Download");

                if (AreMetaAvailable())
                {
                    _logger.LogDebug("Resource meta are available.");

                    try
                    {
                        DownloadMeta(cancellationToken);
                    }
                    catch (DownloadFailureException e)
                    {
                        throw new ResourceMetaDownloadFailureException("Failed to download resource meta.", e);
                    }
                }
                else
                {
                    _logger.LogDebug("Resource meta are not available.");
                }

                if (ShouldUseTorrents())
                {
                    _logger.LogDebug("Torrent downloading is enabled.");

                    try
                    {
                        DownloadWithTorrents(cancellationToken);
                        return;
                    }
                    catch (DownloadFailureException e)
                    {
                        _logger.LogWarning("Failed to download resource with torrents. Falling back to other downloaders...", e);
                    }
                }
                else
                {
                    _logger.LogDebug("Torrent downloading is disabled.");
                }

                if (AreChunksAvailable())
                {
                    _logger.LogDebug("Chunks are available.");

                    try
                    {
                        DownloadWithChunkedHttp(cancellationToken);
                        return;
                    }
                    catch (DownloadFailureException e)
                    {
                        throw new ResourceDownloadFailureException("Failed to download resource.", e);
                    }
                }

                _logger.LogDebug("Chunks are not available.");

                try
                {
                    DownloadWithHttp(cancellationToken);
                }
                catch (DownloadFailureException e)
                {
                    throw new ResourceDownloadFailureException("Failed to download resource.", e);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Downloading resource has failed.", e);
                throw;
            }
        }

        protected virtual void OnDownloadProgressChanged(long downloadedBytes)
        {
            if (DownloadProgressChanged != null) DownloadProgressChanged(downloadedBytes);
        }

        private static IHttpDownloader CreateDefaultHttpDownloader([NotNull] string destinationFilePath,
            [NotNull] string[] urls)
        {
            return new HttpDownloader(destinationFilePath, urls);
        }

        private static IChunkedHttpDownloader CreateDefaultChunkedHttpDownloader([NotNull] string destinationFilePath,
            [NotNull] ResourceUrl[] urls, ChunksData chunksData,
            long size)
        {
            return new ChunkedHttpDownloader(destinationFilePath, urls, chunksData, size);
        }

        private static ITorrentDownloader CreateDefaultTorrentDownloader([NotNull] string destinationFilePath,
            [NotNull] string torrentFilePath,
            long totalBytes)
        {
            return new TorrentDownloader(destinationFilePath, torrentFilePath, totalBytes);
        }
    }
}