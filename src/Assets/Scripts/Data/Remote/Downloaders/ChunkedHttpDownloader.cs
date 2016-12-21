using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Net;

namespace PatchKit.Unity.Patcher.Data.Remote.Downloaders
{
    /// <summary>
    /// Downloads chunk-hashed file through http.
    /// Chunk hashes are used to interrupt and resume downloading if downloaded chunk will be
    /// proven corrupted. In this way even on poor internet connection there's a possibility
    /// of downloading big files through http without the need of re-downloading it again.
    /// </summary>
    internal class ChunkedHttpDownloader : IDisposable
    {
        private const int BufferSize = 1024;
        private readonly byte[] _buffer = new byte[BufferSize];

        private readonly RemoteResource _resource;

        private readonly ChunkedFileStream _chunkedFileStream;

        private bool _started;

        public event DownloadProgressChangedHandler DownloadProgressChanged;

        public ChunkedHttpDownloader(string destinationFilePath, RemoteResource resource)
        {
            ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, errors) => true;
            _resource = resource;

            _chunkedFileStream = new ChunkedFileStream(destinationFilePath, resource.ContentSize, resource.ChunksData, HashFunction);
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

            int retry = 100;

            while (validUrls.Count > 0 && retry > 0)
            {
                for (int i = validUrls.Count - 1; i >= 0 && retry-- > 0; --i)
                {
                    string url = validUrls[i];
                    Status status = TryDownload(url, cancellationToken);

                    DebugLogger.Log(this, "Download of " + url + " exited with status " + status);

                    switch (status)
                    {
                        case Status.Ok:
                            return;
                        case Status.Canceled:
                            throw new OperationCanceledException();
                        case Status.EmptyStream:
                            // try another one
                            break;
                        case Status.CorruptData:
                            // just try another one
                            break;
                        case Status.Timeout:
                            // try another one
                            break;
                        case Status.Other:
                            // try another one
                            break;
                        case Status.NotFound:
                            validUrls.Remove(url);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                DebugLogger.Log(this, "Waiting 10 seconds before trying again...");
                Thread.Sleep(10000);
            }

            if (retry == 0)
            {
                DebugLogger.LogError(this, "Too much retries, aborting...");
            }

            throw new DownloaderException("Unable to download resource.");
        }

        private Status TryDownload(string url, CancellationToken cancellationToken)
        {
            try
            {
                return TryDownloadInner(url, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return Status.Canceled;
            }
            catch (TimeoutException)
            {
                DebugLogger.Log(this, "Got timeout for " + url);
                return Status.Timeout;
            }
            catch (Exception e)
            {
                DebugLogger.LogException(this, e);
                return Status.Other;
            }
        }

        private Status TryDownloadInner(string url, CancellationToken cancellationToken)
        {
            DebugLogger.Log(this, "Trying to download from " + url);

            var offset = CurrentFileSize();

            var webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Method = "GET";
            webRequest.Timeout = 10000;
            webRequest.AddRange(offset);
            DebugLogger.Log(this, "offset: " + offset);

            using (var response = (HttpWebResponse)webRequest.GetResponse())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    DebugLogger.LogError(this, "Resource " + url + " not found (404)");
                    return Status.NotFound;
                }

                if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.PartialContent)
                {
                    DebugLogger.LogError(this, "Resource " + url + " returned status code: " + response.StatusCode);
                    return Status.Other;
                }

                DebugLogger.Log(this, "http content length: " + response.ContentLength);

                using (Stream responseStream = response.GetResponseStream())
                {
                    if (responseStream == null)
                    {
                        DebugLogger.LogError(this, "Empty response stream from " + url);
                        return Status.EmptyStream;
                    }

                    int readBytes;
                    while ((readBytes = responseStream.Read(_buffer, 0, BufferSize)) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        bool retry = !_chunkedFileStream.Write(_buffer, 0, readBytes);

                        if (retry)
                        {
                            return Status.CorruptData;
                        }

                        OnDownloadProgressChanged(CurrentFileSize(), _chunkedFileStream.Length);
                    }

                    OnDownloadProgressChanged(CurrentFileSize(), _chunkedFileStream.Length);
                }
            }

            return _chunkedFileStream.RemainingLength == 0 ? Status.Ok : Status.Other;
        }

        private byte[] HashFunction(byte[] buffer, int offset, int length)
        {
            return HashUtilities.ComputeHash(buffer, offset, length).Reverse().ToArray();
        }

        private long CurrentFileSize()
        {
            if (_chunkedFileStream != null)
            {
                return _chunkedFileStream.VerifiedLength;
            }

            return 0;
        }

        private enum Status
        {
            Ok,
            Canceled,
            EmptyStream,
            CorruptData,
            Timeout,
            NotFound,
            Other,
        }

        public void Dispose()
        {
            _chunkedFileStream.Dispose();
        }

        protected virtual void OnDownloadProgressChanged(long downloadedBytes, long totalBytes)
        {
            if (DownloadProgressChanged != null) DownloadProgressChanged(downloadedBytes, totalBytes);
        }
    }
}
