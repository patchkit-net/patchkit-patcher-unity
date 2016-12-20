using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Ionic.Zip;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Data;
using PatchKit.Unity.Patcher.Statistics;

namespace PatchKit.Unity.Patcher.Net
{
    /// <summary>
    /// Downloads chunk-hashed file through http.
    /// Chunk hashes are used to interrupt and resume downloading if downloaded chunk will be
    /// proven corrupted. In this way even on poor internet connection there's a possibility
    /// of downloading big files through http without the need of re-downloading it again.
    /// </summary>
    class ChunkedFileDownloader : IDisposable {

        private struct DownloadSpeed
        {
            public long Bytes;

            public long Time;

            public DateTime AddTime;
        }

        #region Private Fields

        private readonly string[] _urls;
        private readonly int _chunkSize;

        private const int BufferSize = 1024;
        private readonly byte[] _buffer = new byte[BufferSize];

        private readonly ChunkedFile _chunkedFile;

        private readonly Stopwatch _progressStopwatch = new Stopwatch();
        private long _lastReportedTotalBytes;
        private CustomProgressReporter<DownloadProgress> _progressReporter;

        private bool _started;

        private readonly List<DownloadSpeed> _downloadSpeedList = new List<DownloadSpeed>();

        #endregion

        #region Public Methods

        public ChunkedFileDownloader(string[] urls, long fileSize, string destinationFilePath,
            int chunkSize, string[] chunkHashes) : this(urls, fileSize, destinationFilePath, chunkSize, ToByteArray(chunkHashes))
        {
            // do nothing
        }

        public ChunkedFileDownloader(string[] urls, long fileSize, string destinationFilePath,
            int chunkSize, byte[][] chunkHashes)
        {
            _chunkSize = chunkSize;
            _urls = urls;
            ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, errors) => true;

            _chunkedFile = new ChunkedFile(destinationFilePath, _chunkSize, fileSize,
                        chunkHashes, HashFunction);
        }

        public bool Start(CustomProgressReporter<DownloadProgress> progressReporter, CancellationToken cancellationToken)
        {
            if (_started)
            {
                throw new BadStateException("Cannot start the same ChunkedFileDownloader twice.");
            }
            _started = true;
            _progressReporter = progressReporter;
            _downloadSpeedList.Clear();

            var validUrls = new List<string>(_urls);
            validUrls.Reverse();

            int retry = 100;

            while (validUrls.Count > 0 && retry > 0)
            {
                for (int i = validUrls.Count - 1; i >= 0 && retry-- > 0; --i)
                {
                    string url = validUrls[i];
                    Status status = TryDownload(url, cancellationToken);

                    LogInfo("Download of " + url + " exited with status " + status);

                    switch (status)
                    {
                        case Status.Ok:
                            return true;
                        case Status.Canceled:
                            return false;
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

                LogInfo("Waiting 10 seconds before trying again...");
                Thread.Sleep(10000);
            }

            if (retry == 0)
            {
                LogError("Too much retries, aborting...");
            }

            return false;
        }

        #endregion

        #region Private Methods
        
        private Status TryDownload(string url, CancellationToken cancellationToken)
        {
            try
            {
                return TryDownloadInner(url, cancellationToken);
            } catch (OperationCanceledException)
            {
                return Status.Canceled;
            } catch (TimeoutException)
            {
                LogInfo("Got timeout for " + url);
                return Status.Timeout;
            } catch (Exception e)
            {
                LogException("Got unknown exception", e);
                return Status.Other;
            }
        }

        private Status TryDownloadInner(string url, CancellationToken cancellationToken)
        {
            LogInfo("Trying to download from " + url);

            var offset = CurrentFileSize();

            var webRequest = (HttpWebRequest) WebRequest.Create(url);
            webRequest.Method = "GET";
            webRequest.Timeout = 10000;
            webRequest.AddRange(offset);
            LogInfo("offset: " + offset);

            using (var response = (HttpWebResponse) webRequest.GetResponse())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    LogError("Resource " + url + " not found (404)");
                    return Status.NotFound;
                }

                if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.PartialContent)
                {
                    LogError("Resource " + url + " returned status code: " + response.StatusCode);
                    return Status.Other;
                }

                LogInfo("http content length: " + response.ContentLength);

                using (Stream responseStream = response.GetResponseStream())
                {
                    if (responseStream == null)
                    {
                        LogError("Empty response stream from " + url);
                        return Status.EmptyStream;
                    }

                    int readBytes;
                    while ((readBytes = responseStream.Read(_buffer, 0, BufferSize)) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        bool retry = !_chunkedFile.Write(_buffer, 0, readBytes);

                        if (retry)
                        {
                            return Status.CorruptData;
                        }

                        UpdateProgress();
                    }

                    UpdateProgress(true);
                }
            }

            return _chunkedFile.RemainingLength == 0 ? Status.Ok : Status.Other;
        }

        private byte[] HashFunction(byte[] buffer, int offset, int length)
        {
            return HashUtilities.ComputeHash(buffer, offset, length).Reverse().ToArray();
        }

        private long CurrentFileSize()
        {
            if (_chunkedFile != null)
            {
                return _chunkedFile.VerifiedLength;
            }

            return 0;
        }

        private void UpdateProgress(bool force = false)
        {
            if (force || !_progressStopwatch.IsRunning)
            {
                UpdateProgressNow(0);
                if (!_progressStopwatch.IsRunning)
                {
                    _progressStopwatch.Start();
                }
            } else if (_progressStopwatch.ElapsedMilliseconds > 500)
            {
                UpdateProgressNow(_progressStopwatch.ElapsedMilliseconds);
                _progressStopwatch.Reset();
                _progressStopwatch.Start();
            }

        }

        private void UpdateProgressNow(long elapsedMillis)
        {
            long currentFileSize = CurrentFileSize();

            _downloadSpeedList.Add(new DownloadSpeed
            {
                Bytes = currentFileSize - _lastReportedTotalBytes,
                Time = elapsedMillis,
                AddTime = DateTime.Now
            });

            _downloadSpeedList.RemoveAll(s => (DateTime.Now - s.AddTime).Seconds > 10);

            float kbps = CalculateDownloadSpeedKbps(_downloadSpeedList.Sum(s => s.Bytes),
                        _downloadSpeedList.Sum(s => s.Time));
            float progress = CalculateProgress(currentFileSize, _chunkedFile.Length);

            _lastReportedTotalBytes = currentFileSize;

            LogInfo(string.Format("Downloaded {0} of {1} ({2:P}) at {3} kbps",
                currentFileSize, _chunkedFile.Length, progress, kbps));

            _progressReporter.Progress = new DownloadProgress
            {
                DownloadedBytes = currentFileSize,
                KilobytesPerSecond = kbps,
                Progress = progress,
                TotalBytes = _chunkedFile.Length
            };
        }

        private static float CalculateDownloadSpeedKbps(long bytes, long elapsedMilliseconds)
        {
            if (elapsedMilliseconds == 0)
            {
                return 0.0f;
            }

            return Math.Max(0, (float) (bytes/1024.0/(elapsedMilliseconds/1000.0)));
        }

        private static float CalculateProgress(long downloaded, long total)
        {
            if (total == 0)
            {
                return 0;
            }

            return (float) (downloaded / (double) total);
        }

        private static void LogInfo(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        private static void LogError(string message)
        {
            UnityEngine.Debug.LogError(message);
        }

        private static void LogException(string message, Exception e)
        {
            UnityEngine.Debug.LogError(message);
            UnityEngine.Debug.LogException(e);
        }

        private static byte[][] ToByteArray(string[] chunkHashes)
        {
            var chunks = new byte[chunkHashes.Length][];

            for (int index = 0; index < chunkHashes.Length; index++)
            {
                string hash = chunkHashes[index];
                var array = XXHashToByteArray(hash);

                chunks[index] = array;
            }
            return chunks;
        }

        private static byte[] XXHashToByteArray(string hash)
        {
            while (hash.Length < 8)
            {
                hash = "0" + hash;
            }

            byte[] array = Enumerable.Range(0, hash.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hash.Substring(x, 2), 16))
                .ToArray();
            return array;
        }

        #endregion

        #region Inner Types
        
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

        #endregion

        public void Dispose()
        {
            _chunkedFile.Dispose();
        }
    }
}
