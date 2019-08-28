using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using PatchKit.Logging;
using PatchKit.Network;
using PatchKit.Unity.Patcher.AppData.FileSystem;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Downloads file through HTTP without any validation (such as hash checking).
    /// </summary>
    public sealed class HttpDownloader : IHttpDownloader
    {
        private readonly ILogger _logger;

        // TODO: Use global timeout calculator.
        private readonly IRequestTimeoutCalculator _requestTimeoutCalculator = new SimpleRequestTimeoutCalculator();
        private readonly IRequestRetryStrategy _retryStrategy = new SimpleInfiniteRequestRetryStrategy();

        private readonly string _destinationFilePath;

        private readonly string[] _urls;

        private bool _downloadHasBeenCalled;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public HttpDownloader([NotNull] string destinationFilePath, [NotNull] string[] urls)
        {
            if (destinationFilePath == null) throw new ArgumentNullException("destinationFilePath");
            if (urls == null) throw new ArgumentNullException("urls");

            _logger = PatcherLogManager.DefaultLogger;
            _destinationFilePath = destinationFilePath;
            _urls = urls;
        }

        private FileStream OpenFileStream(CancellationToken cancellationToken)
        {
            var parentDirectory = Path.GetDirectoryName(_destinationFilePath);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                DirectoryOperations.CreateDirectory(parentDirectory, cancellationToken);
            }

            return new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        public void Download(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Downloading...");
                for (int i = 0; i < _urls.Length; i++)
                {
                    _logger.LogTrace("urls[" + i + "] = " + _urls[i]);
                }
                _logger.LogTrace("destinationFilePath = " + _destinationFilePath);

                Assert.MethodCalledOnlyOnce(ref _downloadHasBeenCalled, "Download");

                using (var fileStream = OpenFileStream(cancellationToken))
                {
                    bool retry;

                    do
                    {
                        bool success = _urls.Any(url => TryDownload(url, fileStream, cancellationToken));

                        if (success)
                        {
                            retry = false;
                        }
                        else
                        {
                            _logger.LogWarning("All server requests have failed. Checking if retry is possible...");
                            _retryStrategy.OnRequestFailure();
                            _requestTimeoutCalculator.OnRequestFailure();
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
                _logger.LogError("Downloading has failed.", e);
                throw;
            }
        }

        private bool TryDownload(string url, FileStream fileStream, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug(string.Format("Trying to download from {0}", url));

                fileStream.SetLength(0);
                fileStream.Flush();

                long downloadedBytes = 0;

                const long downloadStatusLogInterval = 5000L;
                var stopwatch = Stopwatch.StartNew();

                var baseHttpDownloader = new BaseHttpDownloader(url, 30000);
                baseHttpDownloader.DataAvailable += (bytes, length) =>
                {
                    fileStream.Write(bytes, 0, length);

                    if (stopwatch.ElapsedMilliseconds > downloadStatusLogInterval)
                    {
                        stopwatch.Reset();
                        stopwatch.Start();

                        _logger.LogDebug(string.Format("Downloaded {0} from {1}", downloadedBytes, url));
                    }

                    downloadedBytes += length;
                    OnDownloadProgressChanged(downloadedBytes);
                };

                baseHttpDownloader.Download(cancellationToken);

                _logger.LogDebug(string.Format("Download from {0} has been successful.", url));

                return true;
            }
            catch (DataNotAvailableException e)
            {
                _logger.LogWarning(string.Format("Unable to download from {0}", url), e);
                return false;
            }
            catch (ServerErrorException e)
            {
                _logger.LogWarning(string.Format("Unable to download from {0}", url), e);
                return false;
            }
            catch (ConnectionFailureException e)
            {
                _logger.LogWarning(string.Format("Unable to download from {0}", url), e);
                return false;
            }
        }

        private void OnDownloadProgressChanged(long downloadedBytes)
        {
            if (DownloadProgressChanged != null)
            {
                DownloadProgressChanged(downloadedBytes);
            }
        }
    }
}