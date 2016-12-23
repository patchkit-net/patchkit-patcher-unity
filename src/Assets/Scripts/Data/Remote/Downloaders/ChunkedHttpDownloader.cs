using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.Data.Remote.Downloaders
{
    /// <summary>
    /// Downloads chunk-hashed file through HTTP.
    /// Chunk hashes are used to interrupt and resume downloading if downloaded chunk will be
    /// proven corrupted. In this way even on poor internet connection there's a possibility
    /// of downloading big files through http without the need of re-downloading it again.
    /// </summary>
    internal class ChunkedHttpDownloader : IDownloader, IDisposable
    {
        private const int RetriesAmount = 100;

        private readonly DebugLogger _debugLogger;

        private readonly RemoteResource _resource;

        private readonly int _timeout;

        private readonly ChunkedFileStream _fileStream;

        private bool _started;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public ChunkedHttpDownloader(string destinationFilePath, RemoteResource resource, int timeout = 10000)
        {
            _debugLogger = new DebugLogger(this);

            _debugLogger.Log("Initialization");
            _debugLogger.LogTrace("destinationFilePath = " + destinationFilePath);
            _debugLogger.LogTrace("timeout = " + timeout);

            _resource = resource;
            _timeout = timeout;

            _fileStream = new ChunkedFileStream(destinationFilePath, resource.ContentSize, resource.ChunksData, HashFunction);
        }

        public void Download(CancellationToken cancellationToken)
        {
            if (_started)
            {
                throw new InvalidOperationException("Cannot start the same ChunkedHttpDownloader twice.");
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
            
            var offset = CurrentFileSize();

            _debugLogger.LogTrace("offset = " + offset);

            BaseHttpDownloader baseHttpDownloader = new BaseHttpDownloader(url, _timeout);
            baseHttpDownloader.RequestCreated += request =>
            {
                request.AddRange(offset);
            };

            baseHttpDownloader.DataDownloaded += (bytes, length) =>
            {
                bool retry = !_fileStream.Write(bytes, 0, length);

                if (retry)
                {
                    throw new DownloaderException("Corrupt data.", DownloaderExceptionStatus.CorruptData);
                }

                OnDownloadProgressChanged(CurrentFileSize(), _resource.ContentSize);
            };

            baseHttpDownloader.Download(cancellationToken);

            if (_fileStream.RemainingLength > 0)
            {
                throw new DownloaderException("Data download hasn't been completed.", DownloaderExceptionStatus.Other);
            }
        }

        private static byte[] HashFunction(byte[] buffer, int offset, int length)
        {
            return HashUtilities.ComputeHash(buffer, offset, length).Reverse().ToArray();
        }

        private long CurrentFileSize()
        {
            if (_fileStream != null)
            {
                return _fileStream.VerifiedLength;
            }

            return 0;
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
