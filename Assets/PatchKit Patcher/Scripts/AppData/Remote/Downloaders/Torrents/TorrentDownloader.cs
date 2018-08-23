﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using JetBrains.Annotations;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Logging;
using PatchKit.Network;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents.Protocol;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents
{
    /// <summary>
    /// Downloads file through torrents by using <see cref="TorrentClient"/>.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public sealed class TorrentDownloader : ITorrentDownloader
    {
        private class RetryStrategy : IRequestRetryStrategy
        {
            private readonly int _tryCount;
            private int _currentTry = 0;

            public RetryStrategy(int count)
            {
                _tryCount = count;
            }

            public void OnRequestSuccess()
            {
                // Do nothing
            }

            public void OnRequestFailure()
            {
                _currentTry++;
            }

            public int DelayBeforeNextTry
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool ShouldRetry
            {
                get
                {
                    return _currentTry <= _tryCount;
                }
            }
        }

        private const int UpdateInterval = 1000;

        private const int ConnectionTimeout = 10000;

        private readonly ILogger _logger;

        private readonly string _destinationFilePath;
        private readonly string _torrentFilePath;
        private readonly long _totalBytes;

        private bool _downloadHasBeenCalled;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        private string DestinationDirectoryPath
        {
            get { return _destinationFilePath + ".torrent_dir"; }
        }

        public TorrentDownloader([NotNull] string destinationFilePath, [NotNull] string torrentFilePath,
            long totalBytes)
        {
            if (destinationFilePath == null) throw new ArgumentNullException("destinationFilePath");
            if (torrentFilePath == null) throw new ArgumentNullException("torrentFilePath");
            if (totalBytes <= 0) throw new ArgumentOutOfRangeException("totalBytes");

            _logger = PatcherLogManager.DefaultLogger;
            _destinationFilePath = destinationFilePath;
            _torrentFilePath = torrentFilePath;
            _totalBytes = totalBytes;
        }

        public void Download(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Downloading...");
                _logger.LogTrace("torrentFilePath = " + _torrentFilePath);
                _logger.LogTrace("destinationFilePath = " + _destinationFilePath);
                _logger.LogTrace("destinationDirectoryPath = " + DestinationDirectoryPath);

                Assert.MethodCalledOnlyOnce(ref _downloadHasBeenCalled, "Download");

                if (!Directory.Exists(DestinationDirectoryPath))
                {
                    DirectoryOperations.CreateDirectory(DestinationDirectoryPath);
                }

                _logger.LogTrace("download dir  = " + DestinationDirectoryPath);

                IRequestRetryStrategy retryStrategy = new RetryStrategy(3);

                do
                {
                    try
                    {
                        DownloadInternal(cancellationToken);
                        break;
                    }
                    catch (TorrentClientException e)
                    {
                        _logger.LogError("Exception in torrent client.", e);
                        retryStrategy.OnRequestFailure();

                        if (!retryStrategy.ShouldRetry)
                        {
                            throw new DownloadFailureException("Failed to download with torrent-client.", e);
                        }
                    }
                } while (retryStrategy.ShouldRetry);

                cancellationToken.ThrowIfCancellationRequested();

                string downloadedFilePath = GetDownloadedFilePath();

                if (File.Exists(_destinationFilePath))
                {
                    File.Delete(_destinationFilePath);
                }

                File.Move(downloadedFilePath, _destinationFilePath);
            }
            catch (Exception e)
            {
                _logger.LogError("Downloading has failed.", e);
                throw;
            }
        }

        private void DownloadInternal(CancellationToken cancellationToken)
        {
            using (var torrentClient = new TorrentClient(new UnityTorrentClientProcessStartInfoProvider()))
            {
                torrentClient.AddTorrent(_torrentFilePath, DestinationDirectoryPath, cancellationToken);

                var timeoutWatch = new Stopwatch();
                timeoutWatch.Start();

                TorrentStatus status = GetAndCheckTorrentStatus(torrentClient, cancellationToken);
                double initialProgress = status.Progress;
                _logger.LogTrace("initialProgress = " + status.Progress);
                var waitHandle = new AutoResetEvent(false);

                OnDownloadProgressChanged(0);

                using (cancellationToken.Register(() => waitHandle.Set()))
                {
                    bool finished = false;

                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        status = GetAndCheckTorrentStatus(torrentClient, cancellationToken);

                        _logger.LogTrace("progress = " + status.Progress);

                        CheckTimeout(timeoutWatch, status.Progress, initialProgress);

                        OnDownloadProgressChanged((long) (_totalBytes * status.Progress));

                        if (status.IsSeeding)
                        {
                            finished = true;
                        }
                        else
                        {
                            waitHandle.WaitOne(UpdateInterval);
                        }
                    } while (!finished);
                }
            }
        }

        private TorrentStatus GetAndCheckTorrentStatus(TorrentClient torrentClient,
            CancellationToken cancellationToken)
        {
            var torrentClientStatus = torrentClient.GetStatus(cancellationToken);

            _logger.LogTrace("status = " + torrentClientStatus.Status);

            if (torrentClientStatus.Status != "ok")
            {
                throw new DownloadFailureException("Torrent client failure.");
            }

            Assert.IsNotNull(torrentClientStatus.Data);
            Assert.IsNotNull(torrentClientStatus.Data.Torrents);
            Assert.AreEqual(1, torrentClientStatus.Data.Torrents.Length);

            var torrentStatus = torrentClientStatus.Data.Torrents[0];

            if (!string.IsNullOrEmpty(torrentStatus.Error))
            {
                throw new DownloadFailureException("Torrent client failure: " + torrentStatus.Error);
            }

            return torrentStatus;
        }

        private void CheckTimeout(Stopwatch timeoutWatch, double progress, double initialProgress)
        {
            if (Math.Abs(progress - initialProgress) > 0.0001)
            {
                return;
            }

            if (timeoutWatch.ElapsedMilliseconds < ConnectionTimeout)
            {
                return;
            }

            throw new DownloadFailureException("Torrent downloading has timed out.");
        }

        private string GetDownloadedFilePath()
        {
            var dirInfo = new DirectoryInfo(DestinationDirectoryPath);

            var dirFiles = dirInfo.GetFiles();

            if (dirFiles.Length != 1)
            {
                throw new DownloadFailureException(string.Format(
                    "Invalid downloaded torrent directory structure. It contains {0} files instead of one.",
                    dirFiles.Length));
            }

            return dirFiles[0].FullName;
        }

        private void OnDownloadProgressChanged(long downloadedbytes)
        {
            var handler = DownloadProgressChanged;
            if (handler != null) handler(downloadedbytes);
        }
    }
}