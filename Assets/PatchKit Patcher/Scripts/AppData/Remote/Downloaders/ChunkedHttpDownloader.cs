using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using PatchKit.Api.Models.Main;
using PatchKit.Logging;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;
using CancellationToken = PatchKit.Unity.Patcher.Cancellation.CancellationToken;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Downloads chunk-hashed file through HTTP.
    /// Chunk hashes are used to interrupt and resume downloading if downloaded chunk will be
    /// proven corrupted. In this way even on poor internet connection there's a possibility
    /// of downloading big files through http without the need of re-downloading it again.
    /// </summary>
    public sealed class ChunkedHttpDownloader : IChunkedHttpDownloader
    {
        private struct DownloadJob
        {
            public string Url;
            public long Offset;
        }

        private readonly ILogger _logger;

        private readonly IRequestTimeoutCalculator _timeoutCalculator = new SimpleRequestTimeoutCalculator();

        private readonly IRequestRetryStrategy _retryStrategy = new SimpleInfiniteRequestRetryStrategy();

        private readonly string _destinationFilePath;

        private readonly ResourceUrl[] _urls;

        private readonly ChunksData _chunksData;

        private readonly long _size;

        private bool _downloadHasBeenCalled;

        private BytesRange _range = new BytesRange(0, -1);

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public ChunkedHttpDownloader([NotNull] string destinationFilePath, [NotNull] ResourceUrl[] urls, ChunksData chunksData,
            long size)
        {
            if (string.IsNullOrEmpty(destinationFilePath))
                throw new ArgumentException("Value cannot be null or empty.", "destinationFilePath");
            if (urls == null) throw new ArgumentNullException("urls");
            if (size <= 0) throw new ArgumentOutOfRangeException("size");

            _logger = PatcherLogManager.DefaultLogger;
            _destinationFilePath = destinationFilePath;
            _urls = urls;
            _chunksData = chunksData;
            _size = size;
        }

        private ChunkedFileStream OpenFileStream()
        {
            var parentDirectory = Path.GetDirectoryName(_destinationFilePath);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            return new ChunkedFileStream(_destinationFilePath, _size, _chunksData,
                HashFunction, ChunkedFileStream.WorkFlags.PreservePreviousFile);
        }

        public void SetRange(BytesRange range)
        {
            _range = range;
        }

        public void Download(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Downloading...");
                _logger.LogTrace("size = " + _size);
                for (int i = 0; i < _urls.Length; i++)
                {
                    _logger.LogTrace("urls[" + i + "].Url = " + _urls[i].Url);
                    _logger.LogTrace("urls[" + i + "].Country = " + _urls[i].Country);
                    _logger.LogTrace("urls[" + i + "].PartSize = " + _urls[i].PartSize);
                }

                _logger.LogTrace("chunksData.ChunkSize = " + _chunksData.ChunkSize);
                _logger.LogTrace("chunksData.Chunks.Length = " + _chunksData.Chunks.Length);

                Assert.MethodCalledOnlyOnce(ref _downloadHasBeenCalled, "Download");

                using (var fileStream = OpenFileStream())
                {
                    bool retry;

                    do
                    {
                        bool success =
                            _urls.Any(url => TryDownload(url, fileStream, cancellationToken));

                        if (success)
                        {
                            retry = false;
                        }
                        else
                        {
                            _logger.LogWarning("All server requests have failed. Checking if retry is possible...");
                            _retryStrategy.OnRequestFailure();
                            _timeoutCalculator.OnRequestFailure();
                            retry = _retryStrategy.ShouldRetry;

                            if (!retry)
                            {
                                throw new DownloadFailureException("Download failure.");
                            }

                            _logger.LogDebug(string.Format("Retry is possible. Waiting {0}ms until before attempt...",
                                _retryStrategy.DelayBeforeNextTry));
                            Threading.CancelableSleep(_retryStrategy.DelayBeforeNextTry, cancellationToken);
                            _logger.LogDebug("Trying to download data once again from each server...");
                        }
                    } while (retry);
                }

                _logger.LogDebug("Downloading finished.");
            }
            catch (Exception e)
            {
                _logger.LogDebug("Download has failed.", e);
                throw;
            }
        }

        private bool TryDownload(ResourceUrl url, ChunkedFileStream fileStream, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug(string.Format("Trying to download from {0}", url.Url));
                _logger.LogTrace("fileStream.VerifiedLength = " + fileStream.VerifiedLength);

                var downloadJobQueue = BuildDownloadJobQueue(url, fileStream.VerifiedLength);
                foreach (var downloadJob in downloadJobQueue)
                {
                    _logger.LogDebug(string.Format("Executing download job {0} with offest {1}", downloadJob.Url,
                        downloadJob.Offset));
                    _logger.LogTrace("fileStream.VerifiedLength = " + fileStream.VerifiedLength);
                    _logger.LogTrace("fileStream.SavedLength = " + fileStream.SavedLength);

                    var baseHttpDownloader = new BaseHttpDownloader(downloadJob.Url, _timeoutCalculator.Timeout);
                    baseHttpDownloader.SetBytesRange(new BytesRange(downloadJob.Offset, -1));

                    const long downloadStatusLogInterval = 5000L;
                    var stopwatch = Stopwatch.StartNew();

                    long downloadedBytes = 0;

                    var job = downloadJob;
                    baseHttpDownloader.DataAvailable += (bytes, length) =>
                    {
                        fileStream.Write(bytes, 0, length);

                        downloadedBytes += length;

                        if (stopwatch.ElapsedMilliseconds > downloadStatusLogInterval)
                        {
                            stopwatch.Reset();
                            stopwatch.Start();

                            _logger.LogDebug(string.Format("Downloaded {0} from {1}", downloadedBytes, job.Url));
                            _logger.LogTrace("fileStream.VerifiedLength = " + fileStream.VerifiedLength);
                            _logger.LogTrace("fileStream.SavedLength = " + fileStream.SavedLength);
                        }

                        OnDownloadProgressChanged(fileStream.VerifiedLength);
                    };

                    baseHttpDownloader.Download(cancellationToken);

                    _logger.LogDebug("Download job execution success.");
                    _logger.LogTrace("fileStream.VerifiedLength = " + fileStream.VerifiedLength);
                    _logger.LogTrace("fileStream.SavedLength = " + fileStream.SavedLength);
                }

                _logger.LogDebug(string.Format("Download from {0} has been successful.", url.Url));

                Assert.AreEqual(0, fileStream.RemainingLength, "Chunks downloading must finish downloading whole file");

                return true;
            }
            catch (InvalidChunkDataException e)
            {
                _logger.LogWarning(string.Format("Unable to download from {0}", url.Url), e);
                return false;
            }
            catch (DataNotAvailableException e)
            {
                _logger.LogWarning(string.Format("Unable to download from {0}", url.Url), e);
                return false;
            }
            catch (ServerErrorException e)
            {
                _logger.LogWarning(string.Format("Unable to download from {0}", url.Url), e);
                return false;
            }
            catch (ConnectionFailureException e)
            {
                _logger.LogWarning(string.Format("Unable to download from {0}", url.Url), e);
                return false;
            }
        }

        private IEnumerable<DownloadJob> BuildDownloadJobQueue(ResourceUrl resourceUrl, long currentOffset)
        {
            long size = _size;
            if (_range.End != -1)
            {
                size = _range.End - _range.Start;
            }
            else
            {
                size = _size - _range.Start;
            }

            long partSize = resourceUrl.PartSize == 0 ? _size : resourceUrl.PartSize;

            int startingPart = (int) (_range.Start / size);
            int partCount = (int) (size / partSize);
            partCount += size % partSize != 0 ? 1 : 0;

            for (int i = startingPart; i < partCount; i++)
            {
                string url = resourceUrl.Url;
                if (i > 0)
                {
                    // second and later indices should have index numebr at the end
                    url += "." + i;
                }

                long offset = Math.Max(currentOffset - partSize * i, 0);
                if (offset < partSize)
                {
                    yield return new DownloadJob {Url = url, Offset = offset};
                }
            }
        }

        private static byte[] HashFunction(byte[] buffer, int offset, int length)
        {
            return HashCalculator.ComputeHash(buffer, offset, length).Reverse().ToArray();
        }

        private void OnDownloadProgressChanged(long downloadedBytes)
        {
            if (DownloadProgressChanged != null) DownloadProgressChanged(downloadedBytes);
        }
    }
}