using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.Data.Remote.Downloaders
{
    /// <summary>
    /// Downloads file through HTTP without any validation (such as hash checking).
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    internal class HttpDownloader : IDownloader, IDisposable
    {
        private const int RetriesAmount = 100;

        private readonly DebugLogger _debugLogger;

        private readonly RemoteResource _resource;

        private readonly int _timeout;

        private readonly FileStream _fileStream;

        private bool _started;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public HttpDownloader(string destinationFilePath, RemoteResource resource, int timeout = 10000)
        {
            _debugLogger = new DebugLogger(this);

            _debugLogger.Log("Initialization");
            _debugLogger.LogTrace("destinationFilePath = " + destinationFilePath);
            _debugLogger.LogTrace("timeout = " + timeout);

            _resource = resource;
            _timeout = timeout;

            _fileStream = new FileStream(destinationFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }

        public void Download(CancellationToken cancellationToken)
        {
            if (_started)
            {
                throw new InvalidOperationException("Cannot start the same HttpDownloader twice.");
            }
            _started = true;

            var validUrls = new List<string>(_resource.ContentUrls);
            validUrls.Reverse();

            int retry = RetriesAmount;

            while (validUrls.Count > 0 && retry > 0)
            {
                for (int i = validUrls.Count - 1; i >= 0 && retry-- > 0; --i)
                {
                    string url = validUrls[i];

                    try
                    {
                        Download(url, cancellationToken);
                        return;
                    }
                    catch (DownloaderException downloaderException)
                    {
                        _debugLogger.LogException(downloaderException);
                        switch (downloaderException.Status)
                        {
                            case DownloaderExceptionStatus.EmptyStream:
                                // try another one
                                break;
                            case DownloaderExceptionStatus.CorruptData:
                                // try another one
                                break;
                            case DownloaderExceptionStatus.NotFound:
                                // remove url and try another one
                                validUrls.Remove(url);
                                break;
                            case DownloaderExceptionStatus.Other:
                                // try another one
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    catch (Exception exception)
                    {
                        _debugLogger.LogException(exception);
                        // try another one
                    }
                }

                _debugLogger.Log("Waiting 10 seconds before trying again...");
                Thread.Sleep(10000);
            }

            if (retry <= 0)
            {
                throw new DownloaderException("Too many retries, aborting.", DownloaderExceptionStatus.Other);
            }

            throw new DownloaderException("Cannot download resource.", DownloaderExceptionStatus.Other);
        }

        private void Download(string url, CancellationToken cancellationToken)
        {
            _debugLogger.Log("Trying to download from " + url);

            ClearFileStream();

            long downloadedBytes = 0;

            BaseHttpDownloader baseHttpDownloader = new BaseHttpDownloader(url, _timeout);
            baseHttpDownloader.DataDownloaded += (bytes, length) =>
            {
                _fileStream.Write(bytes, 0, length);

                downloadedBytes += length;
                OnDownloadProgressChanged(downloadedBytes, _resource.ContentSize);
            };

            baseHttpDownloader.Download(cancellationToken);
        }

        private void ClearFileStream()
        {
            _fileStream.SetLength(0);
            _fileStream.Flush();
        }

        public void Dispose()
        {
            _fileStream.Dispose();
        }

        protected virtual void OnDownloadProgressChanged(long downloadedBytes, long totalBytes)
        {
            if (DownloadProgressChanged != null) DownloadProgressChanged(downloadedBytes, totalBytes);
        }
    }
}