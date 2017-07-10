using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PatchKit.Unity.Patcher.Debug;
using CancellationToken = PatchKit.Unity.Patcher.Cancellation.CancellationToken;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Downloads chunk-hashed file through HTTP.
    /// Chunk hashes are used to interrupt and resume downloading if downloaded chunk will be
    /// proven corrupted. In this way even on poor internet connection there's a possibility
    /// of downloading big files through http without the need of re-downloading it again.
    /// </summary>
    public class ChunkedHttpDownloader : IChunkedHttpDownloader
    {
        private const int RetriesAmount = 100;

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(ChunkedHttpDownloader));

        private readonly string _destinationFilePath;

        private readonly RemoteResource _resource;

        private readonly int _timeout;

        private ChunkedFileStream _fileStream;

        private bool _downloadHasBeenCalled;

        private bool _disposed;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public ChunkedHttpDownloader(string destinationFilePath, RemoteResource resource, int timeout)
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
        }

        private void OpenFileStream()
        {
            if (_fileStream == null)
            {
                _fileStream = new ChunkedFileStream(_destinationFilePath, _resource.Size, _resource.ChunksData,
                    HashFunction, ChunkedFileStream.WorkFlags.PreservePreviousFile);
            }
        }

        private void CloseFileStream()
        {
            if (_fileStream != null)
            {
                _fileStream.Dispose();
                _fileStream = null;
            }
        }

        public void Download(CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _downloadHasBeenCalled, "Download");

            DebugLogger.Log("Downloading.");

            var validUrls = new List<string>(_resource.Urls);
            validUrls.Reverse();

            int retry = RetriesAmount;

            while (validUrls.Count > 0 && retry > 0)
            {
                for (int i = validUrls.Count - 1; i >= 0 && retry-- > 0; --i)
                {
                    string url = validUrls[i];

                    try
                    {
                        OpenFileStream();

                        Download(url, cancellationToken);

                        CloseFileStream();

                        var validator = new DownloadedResourceValidator();
                        validator.Validate(_destinationFilePath, _resource);

                        return;
                    }
                    catch (DownloaderException downloaderException)
                    {
                        DebugLogger.LogException(downloaderException);
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
                    finally
                    {
                        CloseFileStream();
                    }
                }

                DebugLogger.Log("Waiting 10 seconds before trying again...");
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
            DebugLogger.Log(string.Format("Trying to download from {0}", url));
            
            var offset = CurrentFileSize();

            DebugLogger.LogVariable(offset, "offset");

            BaseHttpDownloader baseHttpDownloader = new BaseHttpDownloader(url, _timeout);
            baseHttpDownloader.SetBytesRange(offset);

            baseHttpDownloader.DataAvailable += (bytes, length) =>
            {
                bool retry = !_fileStream.Write(bytes, 0, length);

                if (retry)
                {
                    throw new DownloaderException("Corrupt data.", DownloaderExceptionStatus.CorruptData);
                }

                OnDownloadProgressChanged(CurrentFileSize(), _resource.Size);
            };

            baseHttpDownloader.Download(cancellationToken);
            
            if (_fileStream.RemainingLength > 0)
            {
                throw new DownloaderException("Data download hasn't been completed.", DownloaderExceptionStatus.Other);
            }
        }

        private static byte[] HashFunction(byte[] buffer, int offset, int length)
        {
            return HashCalculator.ComputeHash(buffer, offset, length).Reverse().ToArray();
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ChunkedHttpDownloader()
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

            if(disposing)
            {
                CloseFileStream();
            }

            _disposed = true;
        }

        protected virtual void OnDownloadProgressChanged(long downloadedBytes, long totalBytes)
        {
            if (DownloadProgressChanged != null) DownloadProgressChanged(downloadedBytes, totalBytes);
        }
    }
}
