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
            public DownloadJob(string url, long Start = 0, long End = -1)
            {
                Url = url;
                Range = new BytesRange(Start, End);
            }

            public string Url;
            public BytesRange Range;
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

            int startChunk = (int) (CalculateContainingChunksRange(_range).Start / _chunksData.ChunkSize);
            int endChunk = _range.End == -1 ? -1 : (int) (CalculateContainingChunksRange(_range).End / _chunksData.ChunkSize);

            _logger.LogTrace(string.Format("Opening chunked file stream for chunks {0}-{1}", startChunk, endChunk));
            return new ChunkedFileStream(_destinationFilePath, _size, _chunksData,
                HashFunction, ChunkedFileStream.WorkFlags.PreservePreviousFile, startChunk, endChunk);
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
                        downloadJob.Range.Start));
                    _logger.LogTrace("fileStream.VerifiedLength = " + fileStream.VerifiedLength);
                    _logger.LogTrace("fileStream.SavedLength = " + fileStream.SavedLength);

                    var baseHttpDownloader = new BaseHttpDownloader(downloadJob.Url, _timeoutCalculator.Timeout);
                    baseHttpDownloader.SetBytesRange(downloadJob.Range);

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

        public BytesRange CalculateContainingChunksRange(BytesRange range)
        {
            long chunkSize = _chunksData.ChunkSize;
            long bottom = (range.Start / chunkSize) * chunkSize;

            if (range.End == -1)
            {
                return new BytesRange(bottom, -1);
            }

            long top = (range.End / chunkSize) * chunkSize;

            if (top < range.End)
            {
                top += chunkSize;
            }

            Assert.IsTrue(top >= range.End && bottom <= range.Start, "Effective range must contain the original range.");

            return new BytesRange(bottom, top);
        }

        private IEnumerable<DownloadJob> BuildDownloadJobQueue(ResourceUrl resourceUrl, long currentOffset)
        {
            _logger.LogDebug("Building download jobs.");
            var effectiveRange = CalculateContainingChunksRange(_range);
            long lowerBound = Math.Max(currentOffset, effectiveRange.Start);
            long upperBound = effectiveRange.End != -1 ? effectiveRange.End - 1 : -1;
            long effectiveDataSize = upperBound != -1 ? upperBound - lowerBound + 1 : _size - lowerBound;

            if (resourceUrl.PartSize == 0)
            {
                _logger.LogDebug("No parts, returning a single download job");
                yield return new DownloadJob(resourceUrl.Url, lowerBound, upperBound);
                yield break;
            }

            long partSize = resourceUrl.PartSize;

            int startingPart = (int) (lowerBound / partSize);
            int partCount = (int) (effectiveDataSize / partSize) + 1;

            _logger.LogDebug(string.Format("Download jobs will be separated into {0} parts, starting at {1}.", partCount, startingPart));
            for (int i = startingPart; i < startingPart + partCount; i++)
            {
                string url = resourceUrl.Url;
                if (i > 0)
                {
                    // second and later indices should have index numebr at the end
                    url += "." + i;
                }

                long partBottom = i * partSize;
                long partTop = (i+1) * partSize - 1;

                long localLowerBound = lowerBound < partBottom ? 0 : lowerBound - partBottom;
                long localUpperBound = upperBound > partTop ? -1 : upperBound - partBottom;

                long effectivePartSize = localUpperBound != -1 ? (localUpperBound - localLowerBound) : (partSize - localLowerBound);

                if (effectivePartSize > 0)
                {
                    yield return new DownloadJob(url, localLowerBound, localUpperBound);
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