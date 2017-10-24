using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Downloads file through HTTP without any validation (such as hash checking).
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class HttpDownloader : IHttpDownloader
    {
        private const int RetriesAmount = 100;

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(HttpDownloader));

        private readonly string _destinationFilePath;

        private readonly RemoteResource? _resource;

        private readonly string[] _mirrorUrls;

        private readonly long _size;

        private readonly int _timeout;

        private FileStream _fileStream;

        private bool _downloadHasBeenCalled;

        private bool _disposed;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public HttpDownloader(string destinationFilePath, RemoteResource resource, int timeout)
            : this(destinationFilePath, resource.GetUrls(), resource.Size, timeout)
        {
            _resource = resource;
        }

        public HttpDownloader(string destinationFilePath, string[] mirrorUrls, long size, int timeout)
        {
            Checks.ArgumentParentDirectoryExists(destinationFilePath, "destinationFilePath");
            Checks.ArgumentMoreThanZero(timeout, "timeout");
            Checks.ArgumentNotNull(mirrorUrls, "mirrorUrls");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(destinationFilePath, "destinationFilePath");
            DebugLogger.LogVariable(mirrorUrls, "mirrorUrls");
            DebugLogger.LogVariable(size, "size");
            DebugLogger.LogVariable(timeout, "timeout");

            _destinationFilePath = destinationFilePath;
            _mirrorUrls = mirrorUrls;
            _size = size;
            _timeout = timeout;
        }

        private void OpenFileStream()
        {
            if (_fileStream == null)
            {
                _fileStream = new FileStream(_destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
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

            var validUrls = new List<string>(_mirrorUrls);
            
            // getting through urls list backwards, because urls may be removed during the process,
            // and it's easier to iterate that way
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

                        if (_resource.HasValue)
                        {
                            var validator = new DownloadedResourceValidator();
                            validator.Validate(_destinationFilePath, _resource.Value);
                        }

                        return;
                    }
                    catch (DownloadedResourceValidationException validationException)
                    {
                        DebugLogger.LogException(validationException);
                        validUrls.Remove(url);
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
                Threading.CancelableSleep(10000, cancellationToken);
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

            ClearFileStream();

            long downloadedBytes = 0;

            BaseHttpDownloader baseHttpDownloader = new BaseHttpDownloader(url, _timeout);
            baseHttpDownloader.DataAvailable += (bytes, length) =>
            {
                _fileStream.Write(bytes, 0, length);

                downloadedBytes += length;
                OnDownloadProgressChanged(downloadedBytes, _size);
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~HttpDownloader()
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
            if (DownloadProgressChanged != null)
            {
                DownloadProgressChanged(downloadedBytes, totalBytes);
            }
        }
    }
}