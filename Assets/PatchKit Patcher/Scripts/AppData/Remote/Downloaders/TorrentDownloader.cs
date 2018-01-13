using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using JetBrains.Annotations;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents.Protocol;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Downloads file through torrents by using <see cref="TorrentClient"/>.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public sealed class TorrentDownloader : ITorrentDownloader
    {
        private const int UpdateInterval = 1000;

        private const int ConnectionTimeout = 10000;

        private ILogger _logger;

        private readonly string _destinationFilePath;
        private readonly string _torrentFilePath;

        private bool _downloadHasBeenCalled;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        private string DestinationDirectoryPath
        {
            get { return _destinationFilePath + ".torrent_dir"; }
        }

        public TorrentDownloader([NotNull] string destinationFilePath, [NotNull] string torrentFilePath)
        {
            if (destinationFilePath == null) throw new ArgumentNullException("destinationFilePath");
            if (torrentFilePath == null) throw new ArgumentNullException("torrentFilePath");

            _logger = PatcherLogManager.DefaultLogger;
            _destinationFilePath = destinationFilePath;
            _torrentFilePath = torrentFilePath;
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

                using (var torrentClient = new TorrentClient(new UnityTorrentClientProcessStartInfoProvider()))
                {
                    using (var tempDir = new TemporaryDirectory(DestinationDirectoryPath))
                    {
                        torrentClient.AddTorrent(_torrentFilePath, tempDir.Path, cancellationToken);

                        var timeoutWatch = new Stopwatch();
                        timeoutWatch.Start();

                        var status = GetAndCheckTorrentStatus(torrentClient, cancellationToken);
                        double initialProgress = status.Progress;
                        var waitHandle = new AutoResetEvent(false);

                        using (cancellationToken.Register(() => waitHandle.Set()))
                        {
                            bool finished = false;

                            do
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                status = GetAndCheckTorrentStatus(torrentClient, cancellationToken);

                                CheckTimeout(timeoutWatch, status.Progress, initialProgress);

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

                        cancellationToken.ThrowIfCancellationRequested();

                        var downloadedFilePath = GetDownloadedFilePath();

                        if (File.Exists(_destinationFilePath))
                        {
                            File.Delete(_destinationFilePath);
                        }

                        File.Move(downloadedFilePath, _destinationFilePath);
                    }
                }

                // TODO: move file
            }
            catch (Exception e)
            {
                _logger.LogError("Downloading has failed.", e);
                throw;
            }
        }

        private TorrentStatus GetAndCheckTorrentStatus(TorrentClient torrentClient, CancellationToken cancellationToken)
        {
            var torrentClientStatus = torrentClient.GetStatus(cancellationToken);

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

        //private void UpdateTorrentProgress(double progress)
        //{
        //    OnDownloadProgressChanged((long) (_resource.Size * progress));
        //}

        private void OnDownloadProgressChanged(long downloadedBytes)
        {
            if (DownloadProgressChanged != null) DownloadProgressChanged(downloadedBytes);
        }
    }
}