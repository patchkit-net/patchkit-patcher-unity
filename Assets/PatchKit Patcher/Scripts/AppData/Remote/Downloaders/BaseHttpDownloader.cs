using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using PatchKit.Logging;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;
using UnityEngine.Networking;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public sealed class BaseHttpDownloader : IBaseHttpDownloader
    {
        private class Handler : DownloadHandlerScript
        {
            DataAvailableHandler _receiveData;

            public Handler(DataAvailableHandler receiveData)
            {
                _receiveData = receiveData;
            }

            protected override bool ReceiveData(byte[] data, int dataLength)
            {
                _receiveData(data, dataLength);

                return true;
            }
        }

        private readonly ILogger _logger;

        private static readonly ulong DefaultBufferSize = 5 * (ulong)Units.MB;

        private readonly string _url;
        private readonly int _timeout;

        private readonly ulong _bufferSize;
        private readonly byte[] _buffer;

        private bool _downloadHasBeenCalled;
        private BytesRange? _bytesRange;

        public BaseHttpDownloader(string url, int timeout) :
            this(url, timeout, PatcherLogManager.DefaultLogger, DefaultBufferSize)
        {
        }

        public BaseHttpDownloader([NotNull] string url, int timeout, ulong bufferSize)
            : this(url, timeout, PatcherLogManager.DefaultLogger, bufferSize)
        {
        }

        public BaseHttpDownloader([NotNull] string url, int timeout,
            [NotNull] ILogger logger, ulong bufferSize)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentException("Value cannot be null or empty.", "url");
            if (timeout <= 0) throw new ArgumentOutOfRangeException("timeout");
            if (logger == null) throw new ArgumentNullException("logger");

            _url = url;
            _timeout = timeout;
            _logger = logger;

            _bufferSize = bufferSize;
            _buffer = new byte[_bufferSize];

            ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, errors) => true;
            ServicePointManager.DefaultConnectionLimit = 65535;
        }

        public void SetBytesRange(BytesRange? range)
        {
            _bytesRange = range;

            if (_bytesRange.HasValue && _bytesRange.Value.Start == 0 && _bytesRange.Value.End == -1)
            {
                _bytesRange = null;
            }
        }

        public IEnumerable<DataPacket> ReadPackets(CancellationToken cancellationToken)
        {
            CancellationTokenSource localCancel = new CancellationTokenSource();
            cancellationToken.Register(() => localCancel.Cancel());

            var packetQueue = new Queue<DataPacket>();
            bool isDone = false;

            Exception threadException = null;

            var threadHandle = new Thread(_ =>
            {
                try
                {
                    Download(localCancel.Token, (data, length) =>
                    {
                        lock (packetQueue)
                        {
                            packetQueue.Enqueue(new DataPacket
                            {
                                Data = data,
                                Length = length,
                            });
                        }
                    });
                }
                catch (OperationCanceledException e)
                {
                    if (cancellationToken.IsCancelled)
                    {
                        threadException = e;
                    }
                }
                catch (Exception e)
                {
                    threadException = e;
                }

                isDone = true;
            });

            Func<int> safeCount = () => {
                lock (packetQueue)
                {
                    return packetQueue.Count;
                }
            };

            try
            {
                threadHandle.Start();
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (safeCount() > 0)
                    {
                        lock (packetQueue)
                        {
                            foreach (var packet in packetQueue)
                            {
                                yield return packet;
                            }
                            packetQueue.Clear();
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                } while (!isDone);
            }
            finally
            {
                localCancel.Cancel();
                threadHandle.Join();

                if (threadException != null)
                {
                    throw threadException;
                }
            }
        }

        public void Download(CancellationToken cancellationToken, [NotNull] DataAvailableHandler onDataAvailable)
        {
            if (onDataAvailable == null)
            {
                throw new ArgumentNullException("onDataAvailable");
            }

            try
            {
                _logger.LogDebug("Downloading...");
                _logger.LogTrace("url = " + _url);
                _logger.LogTrace("bufferSize = " + _bufferSize);
                _logger.LogTrace("bytesRange = " + (_bytesRange.HasValue
                                     ? _bytesRange.Value.Start + "-" + _bytesRange.Value.End
                                     : "(none)"));
                _logger.LogTrace("timeout = " + _timeout);

                Assert.MethodCalledOnlyOnce(ref _downloadHasBeenCalled, "Download");

                UnityWebRequest request = null;

                UnityDispatcher.Invoke(() =>
                {
                    request = new UnityWebRequest();
                    request.uri = new Uri(_url);
                    request.timeout = 30;

                    if (_bytesRange.HasValue)
                    {
                        var bytesRangeEndText =
                            _bytesRange.Value.End >= 0L ? _bytesRange.Value.End.ToString() : string.Empty;

                        request.SetRequestHeader(
                            "Range",
                            "bytes=" + _bytesRange.Value.Start + "-" + bytesRangeEndText);
                    }

                    request.downloadHandler = new Handler(onDataAvailable);
                }).WaitOne();

                using (request)
                {
                    using (request.downloadHandler)
                    {
                        UnityWebRequestAsyncOperation op = null;

                        UnityDispatcher.Invoke(() =>
                        {
                            op = request.SendWebRequest();
                        }).WaitOne();

                        long requestResponseCode = -1;

                        while (requestResponseCode <= 0)
                        {
                            UnityDispatcher.Invoke(() =>
                            {
                                requestResponseCode = request.responseCode;
                            }).WaitOne();

                            cancellationToken.ThrowIfCancellationRequested();

                            System.Threading.Thread.Sleep(100);
                        }

                        _logger.LogDebug("Received response from server.");
                        _logger.LogTrace("statusCode = " + requestResponseCode);

                        if (Is2XXStatus((HttpStatusCode)requestResponseCode))
                        {
                            _logger.LogDebug("Successful response. Reading response stream...");

                            bool opIsDone = false;

                            while (!opIsDone)
                            {
                                UnityDispatcher.Invoke(() =>
                                {
                                    opIsDone = op.isDone;
                                }).WaitOne();

                                cancellationToken.ThrowIfCancellationRequested();

                                System.Threading.Thread.Sleep(100);
                            }

                            _logger.LogDebug("Stream has been read.");
                        }
                        else if (Is4XXStatus((HttpStatusCode)requestResponseCode))
                        {
                            throw new DataNotAvailableException(string.Format(
                                "Request data for {0} is not available (status: {1})", _url, (HttpStatusCode)request.responseCode));
                        }
                        else
                        {
                            throw new ServerErrorException(string.Format(
                                "Server has experienced some issues with request for {0} which resulted in {1} status code.",
                                _url, (HttpStatusCode)requestResponseCode));
                        }
                    }
                }

                _logger.LogDebug("Downloading finished.");
            }
            catch (WebException webException)
            {
                _logger.LogError("Downloading has failed.", webException);
                throw new ConnectionFailureException(
                    string.Format("Connection to server has failed while requesting {0}", _url), webException);
            }
            catch (Exception e)
            {
                _logger.LogError("Downloading has failed.", e);
                throw;
            }
        }

        private static bool Is2XXStatus(HttpStatusCode statusCode)
        {
            return (int)statusCode >= 200 && (int)statusCode <= 299;
        }

        // ReSharper disable once InconsistentNaming
        private static bool Is4XXStatus(HttpStatusCode statusCode)
        {
            return (int)statusCode >= 400 && (int)statusCode <= 499;
        }
    }
}