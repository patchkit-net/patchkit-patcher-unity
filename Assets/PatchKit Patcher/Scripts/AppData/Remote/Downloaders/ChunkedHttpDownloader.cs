using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PatchKit.Api.Models.Main;
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

            List<ResourceUrl> validUrls = new List<ResourceUrl>(_resource.ResourceUrls);
            
            // getting through urls list backwards, because urls may be removed during the process,
            // and it's easier to iterate that way
            validUrls.Reverse();

            int retry = RetriesAmount;

            while (validUrls.Count > 0 && retry > 0)
            {
                for (int i = validUrls.Count - 1; i >= 0 && retry-- > 0; --i)
                {
                    ResourceUrl url = validUrls[i];

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

        private void Download(ResourceUrl url, CancellationToken cancellationToken)
        {
            DebugLogger.Log(string.Format("Trying to download from {0}", url));
            
            long offset = CurrentFileSize();

            DebugLogger.LogVariable(offset, "offset");

            var downloadJobQueue = BuildDownloadJobQueue(url, offset);
            foreach (DownloadJob downloadJob in downloadJobQueue)
            {
                BaseHttpDownloader baseHttpDownloader = new BaseHttpDownloader(downloadJob.Url, _timeout);
                baseHttpDownloader.SetBytesRange(downloadJob.Offset);
                
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
            }
            
            if (_fileStream.RemainingLength > 0)
            {
                throw new DownloaderException("Data download hasn't been completed.", DownloaderExceptionStatus.Other);
            }
        }

        /// <summary>
        /// Builds downloads queue based on url, part sizes, file size and current offset.
        /// </summary>
        /// <param name="resourceUrl"></param>
        /// <param name="currentOffset"></param>
        /// <returns></returns>
        private List<DownloadJob> BuildDownloadJobQueue(ResourceUrl resourceUrl, long currentOffset)
        {
            long totalSize = _resource.Size;
            long partSize = resourceUrl.PartSize == 0 ? totalSize : resourceUrl.PartSize;
            
            int partCount = (int) (totalSize / partSize);
            partCount += totalSize % partSize != 0 ? 1 : 0;
            
            List<DownloadJob> queue = new List<DownloadJob>();
            for (int i = 0; i < partCount; i++)
            {
                string url = resourceUrl.Url;
                if (i > 0)
                {
                    // second and later indices should have index numebr at the end
                    url += "." + i;
                }
                long offset = Math.Max(currentOffset - partSize * i, 0);
                if (offset < partSize)
                {
                    queue.Add(new DownloadJob {Url = url, Offset = offset});
                }
            }

            return queue;
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
        
        private struct DownloadJob
        {
            public string Url { get; set; }
            public long Offset { get; set; }
        }
    }
}
