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

        private readonly ILogger _logger;

        private readonly string _destinationFilePath;
        private readonly string _destinationMetaPath;

        private readonly RemoteResource _resource;

        private readonly CreateNewHttpDownloader _createNewHttpDownloader;
        private readonly CreateNewChunkedHttpDownloader _createNewChunkedHttpDownloader;

        private bool _downloadHasBeenCalled;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public RemoteResourceDownloader(string destinationFilePath, string destinationMetaPath, RemoteResource resource)
            : this(destinationFilePath, destinationMetaPath, resource, CreateDefaultHttpDownloader,
                CreateDefaultChunkedHttpDownloader)
        {
        }

        public RemoteResourceDownloader([NotNull] string destinationFilePath, [NotNull] string destinationMetaPath,
            RemoteResource resource,
            CreateNewHttpDownloader createNewHttpDownloader,
            CreateNewChunkedHttpDownloader createNewChunkedHttpDownloader)
        {
            if (destinationFilePath == null) throw new ArgumentNullException("destinationFilePath");
            if (destinationMetaPath == null) throw new ArgumentNullException("destinationMetaPath");

            _logger = PatcherLogManager.DefaultLogger;
            _destinationFilePath = destinationFilePath;
            _destinationMetaPath = destinationMetaPath;
            _resource = resource;
            _createNewHttpDownloader = createNewHttpDownloader;
            _createNewChunkedHttpDownloader = createNewChunkedHttpDownloader;
        }

        private void DownloadMeta(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Downloading resource meta...");

            var downloader = _createNewHttpDownloader(_destinationMetaPath, _resource.GetMetaUrls());
            downloader.Download(cancellationToken);

            _logger.LogDebug("Resource meta downloaded.");
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
    }
}