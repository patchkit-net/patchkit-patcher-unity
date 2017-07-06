using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Downloads file through torrents by using <see cref="TorrentClient"/>.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class TorrentDownloader : ITorrentDownloader
    {
        private const int UpdateInterval = 1000;

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(TorrentDownloader));

        private readonly string _destinationFilePath;

        private readonly RemoteResource _resource;

        private readonly int _timeout;

        private readonly Stopwatch _timeoutWatch;

        private TorrentClient _torrentClient;

        private double _lastProgress;

        private bool _downloadHasBeenCalled;

        private bool _disposed;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public TorrentDownloader(string destinationFilePath, RemoteResource resource, int timeout)
        {
            Checks.ArgumentParentDirectoryExists(destinationFilePath, "destinationFilePath");
            Checks.ArgumentValidRemoteResource(resource, "resource");
            Checks.ArgumentMoreThanZero(timeout, "timeout");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(destinationFilePath, "destinationFilePath");
            DebugLogger.LogVariable(resource, "resource");
            DebugLogger.LogVariable(timeout, "timeout");

            _destinationFilePath = destinationFilePath;
            _resource = resource;
            _timeout = timeout;

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
            DebugLogger.Log("Downloading torrent file");

            try
            {
                using (var torrentFileStream = new FileStream(TorrentFilePath, FileMode.Create))
                {
                    var baseHttpDownloader = new BaseHttpDownloader(_resource.TorrentUrls[0], _timeout);
                    baseHttpDownloader.DataAvailable += (data, length) =>
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        torrentFileStream.Write(data, 0, length);
                    };
                    baseHttpDownloader.Download(cancellationToken);
                }
            }
            catch (WebException exception)
            {
                DebugLogger.LogException(exception);
                throw new DownloaderException("Unable to download torrent file.", DownloaderExceptionStatus.Other);
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
            DebugLogger.Log("Adding torrent.");
            
            string convertedTorrentFilePath = ConvertPathForTorrentClient(TorrentFilePath);
            string convertedDownloadDirectoryPath = ConvertPathForTorrentClient(DownloadDirectoryPath);

            DebugLogger.LogVariable(convertedTorrentFilePath, "convertedTorrentFilePath");
            DebugLogger.LogVariable(convertedDownloadDirectoryPath, "convertedDownloadDirectoryPath");

            string command = string.Format("add-torrent {0} {1}", convertedTorrentFilePath,
                convertedDownloadDirectoryPath);

            var result = _torrentClient.ExecuteCommand(command);

            DebugLogger.LogVariable(result, "result");

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
                throw new DownloaderException("Torrent download has timed out.", DownloaderExceptionStatus.Other);
            }
        }

        private void UpdateTorrentProgress(double progress)
        {
            OnDownloadProgressChanged((long)(_resource.Size * progress), _resource.Size);
        }

        private bool UpdateTorrentStatus()
        {
            var result = _torrentClient.ExecuteCommand("status");

            DebugLogger.LogVariable(result, "result");

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
            DebugLogger.Log("Moving downloaded file to " + _destinationFilePath);

            var dirInfo = new DirectoryInfo(DownloadDirectoryPath);

            var dirFiles = dirInfo.GetFiles();

            DebugLogger.LogVariable(dirFiles.Length, "dirFiles.Length");

            if (dirFiles.Length < 1)
            {
                throw new DownloaderException("Missing files in downloaded torrent directory.", DownloaderExceptionStatus.Other);
            }

            if (File.Exists(_destinationFilePath))
            {
                FileOperations.Delete(_destinationFilePath);
            }

            DebugLogger.LogVariable(dirFiles[0].FullName, "dirFiles[0].FullName");

            FileOperations.Move(dirFiles[0].FullName, _destinationFilePath);
        }

        private void Cleanup()
        {
            DebugLogger.Log("Cleaning up...");

            if (Directory.Exists(DownloadDirectoryPath))
            {
                SafeInvoker.Invoke(() => DirectoryOperations.Delete(DownloadDirectoryPath, true), null, _ =>
                {
                    DebugLogger.LogWarning("Unable to cleanup torrent download directory.");
                });
            }

            if (File.Exists(TorrentFilePath))
            {
                SafeInvoker.Invoke(() => FileOperations.Delete(TorrentFilePath), null, _ =>
                {
                    DebugLogger.LogWarning("Unable to cleanup torrent file.");
                });
            }

            DebugLogger.Log("Cleanup completed.");
        }

        public void Download(CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _downloadHasBeenCalled, "Download");

            DebugLogger.Log("Downloading.");

            _lastProgress = 0.0;

            try
            {
                _torrentClient = new TorrentClient(new UnityTorrentClientProcessStartInfoProvider());
                DownloadTorrentFile(cancellationToken);
                AddTorrent();
                WaitForTorrentDownload(cancellationToken);
                MoveDownloadedFile();
            }
            finally
            {
                if (_torrentClient != null)
                {
                    _torrentClient.Dispose();
                }
                Cleanup();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TorrentDownloader()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(_disposed)
            {
                return;
            }

            DebugLogger.LogDispose();

            if (disposing && _torrentClient != null)
            {
                _torrentClient.Dispose();
            }

            _disposed = true;
        }

        protected virtual void OnDownloadProgressChanged(long downloadedBytes, long totalBytes)
        {
            if (DownloadProgressChanged != null) DownloadProgressChanged(downloadedBytes, totalBytes);
        }
    }
}