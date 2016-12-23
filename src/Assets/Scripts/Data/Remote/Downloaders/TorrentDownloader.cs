using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Data.Remote.Downloaders
{
    /// <summary>
    /// Downloads file through torrents by using <see cref="TorrentClient"/>.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    internal class TorrentDownloader : IDownloader, IDisposable
    {
        private const int UpdateInterval = 1000;

        private readonly DebugLogger _debugLogger;

        private readonly string _destinationFilePath;

        private readonly RemoteResource _resource;

        private readonly int _timeout;

        private readonly TorrentClient _torrentClient;

        private readonly Stopwatch _timeoutWatch;

        private double _lastProgress;

        private bool _started;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public TorrentDownloader(string destinationFilePath, RemoteResource resource, int timeout = 10000)
        {
            _debugLogger = new DebugLogger(this);

            _debugLogger.Log("Initialization");
            _debugLogger.LogTrace("destinationFilePath = " + destinationFilePath);
            _debugLogger.LogTrace("timeout = " + timeout);

            _destinationFilePath = destinationFilePath;
            _resource = resource;
            _timeout = timeout;

            _torrentClient = new TorrentClient();

            _timeoutWatch = new Stopwatch();
        }

        private string DownloadDirectoryPath
        {
            get
            {
                return _destinationFilePath + ".torrent_dir";
            }
        }

        private string TorrentFilePath
        {
            get
            {
                return _destinationFilePath + ".torrent";
            }
        }

        private void DownloadTorrentFile(CancellationToken cancellationToken)
        {
            _debugLogger.Log("Downloading torrent file.");

            var torrentFileResouce = new RemoteResource();
            Array.Copy(_resource.TorrentUrls, torrentFileResouce.ContentUrls, _resource.TorrentUrls.Length);

            using (var httpDownloader = new HttpDownloader(TorrentFilePath, torrentFileResouce, _timeout))
            {
                httpDownloader.Download(cancellationToken);
            }
        }

        private string ConvertPathForTorrentClient(string path)
        {
            return path.Replace("\\", "/").Replace(" ", "\\ ");
        }

        private void VerifyAddTorrentResult(JToken result)
        {
            if (result.Value<string>("status") != "ok")
            {
                throw new DownloaderException("Cannot add torrent to torrent-client.", DownloaderExceptionStatus.Other);
            }
        }

        private void AddTorrent()
        {
            _debugLogger.Log("Adding torrent.");
            
            string convertedTorrentFilePath = ConvertPathForTorrentClient(TorrentFilePath);
            string convertedDownloadDirectoryPath = ConvertPathForTorrentClient(DownloadDirectoryPath);

            _debugLogger.LogTrace("convertedTorrentFilePath = " + convertedTorrentFilePath);
            _debugLogger.LogTrace("convertedDownloadDirectoryPath = " + convertedDownloadDirectoryPath);

            string command = string.Format("add-torrent {0} {1}", convertedTorrentFilePath,
                convertedDownloadDirectoryPath);

            var result = _torrentClient.ExecuteCommand(command);

            _debugLogger.LogTrace(result);

            VerifyAddTorrentResult(result);

            _timeoutWatch.Reset();
            _timeoutWatch.Start();
        }

        private void CheckTimeout(double progress)
        {
            if (progress > _lastProgress)
            {
                _timeoutWatch.Reset();
                _timeoutWatch.Start();

                _lastProgress = progress;
            }

            if (_timeoutWatch.ElapsedMilliseconds > _timeout)
            {
                throw new TimeoutException("Torrent download has timed out.");
            }
        }

        private void UpdateTorrentProgress(double progress)
        {
            OnDownloadProgressChanged(Mathf.CeilToInt(_resource.ContentSize * (float)progress), _resource.ContentSize);
        }

        private bool UpdateTorrentStatus()
        {
            var result = _torrentClient.ExecuteCommand("status");

            _debugLogger.LogTrace(result);

            if (result.Value<string>("status") != "ok")
            {
                throw new DownloaderException("Invalid torrent-client status - " + result.Value<string>("status"), DownloaderExceptionStatus.Other);
            }

            if (result["data"].Value<int>("count") < 1)
            {
                throw new DownloaderException("Torrent download is not listed.", DownloaderExceptionStatus.Other);
            }

            var torrentStatus = result["data"].Value<JArray>("torrents")[0];

            if (torrentStatus.Value<string>("error") != string.Empty)
            {
                throw new DownloaderException(torrentStatus.Value<string>("error"), DownloaderExceptionStatus.Other);
            }

            double progress = torrentStatus.Value<double>("progress");

            CheckTimeout(progress);
            UpdateTorrentProgress(progress);

            if (torrentStatus.Value<bool>("is_seeding"))
            {
                return true;
            }

            return false;
        }

        private void WaitForTorrentDownload(CancellationToken cancellationToken)
        {
            bool downloaded = false;

            var waitHandle = new AutoResetEvent(false);

            using (cancellationToken.Register(() => waitHandle.Set()))
            {
                while (!downloaded)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (UpdateTorrentStatus())
                    {
                        downloaded = true;
                    }

                    waitHandle.WaitOne(UpdateInterval);
                }
            }
        }

        private void MoveDownloadedFile()
        {
            _debugLogger.Log("Moving downloaded file to " + _destinationFilePath);

            var dirInfo = new DirectoryInfo(DownloadDirectoryPath);

            var dirFiles = dirInfo.GetFiles();

            _debugLogger.LogTrace("dirFiles.Length = " + dirFiles.Length);

            if (dirFiles.Length < 1)
            {
                throw new DownloaderException("Missing files in downloaded torrent directory.", DownloaderExceptionStatus.Other);
            }

            if (File.Exists(_destinationFilePath))
            {
                File.Delete(_destinationFilePath);
            }

            _debugLogger.LogTrace("dirFiles[0].FullName = " + dirFiles[0].FullName);

            File.Move(dirFiles[0].FullName, _destinationFilePath);
        }

        private void Cleanup()
        {
            if (Directory.Exists(DownloadDirectoryPath))
            {
                Directory.Delete(DownloadDirectoryPath, true);
            }

            if (File.Exists(TorrentFilePath))
            {
                File.Delete(TorrentFilePath);
            }
        }

        public void Download(CancellationToken cancellationToken)
        {
            if (_started)
            {
                throw new InvalidOperationException("Cannot start the same TorrentDownloader twice.");
            }
            _started = true;
            _lastProgress = 0.0;

            try
            {
                DownloadTorrentFile(cancellationToken);
                AddTorrent();
                WaitForTorrentDownload(cancellationToken);
                MoveDownloadedFile();
            }
            finally
            {
                Cleanup();
            }
        }

        public void Dispose()
        {
            _torrentClient.Dispose();
        }

        protected virtual void OnDownloadProgressChanged(long downloadedBytes, long totalBytes)
        {
            if (DownloadProgressChanged != null) DownloadProgressChanged(downloadedBytes, totalBytes);
        }
    }
}