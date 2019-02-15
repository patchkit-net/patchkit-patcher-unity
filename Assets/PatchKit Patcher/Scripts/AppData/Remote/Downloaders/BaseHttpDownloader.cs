using System;
using System.IO;
using System.Net;
using JetBrains.Annotations;
using PatchKit.Logging;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public sealed class BaseHttpDownloader : IBaseHttpDownloader
    {
        private readonly ILogger _logger;

        private static readonly int BufferSize = 5 * (int) Units.MB;

        private readonly string _url;
        private readonly int _timeout;
        private readonly IHttpClient _httpClient;

        private readonly byte[] _buffer;

        private bool _downloadHasBeenCalled;
        private BytesRange? _bytesRange;

        public event DataAvailableHandler DataAvailable;

        public BaseHttpDownloader(string url, int timeout) :
            this(url, timeout, new DefaultHttpClient(), PatcherLogManager.DefaultLogger)
        {
        }

        public BaseHttpDownloader([NotNull] string url, int timeout, [NotNull] IHttpClient httpClient,
            [NotNull] ILogger logger)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentException("Value cannot be null or empty.", "url");
            if (timeout <= 0) throw new ArgumentOutOfRangeException("timeout");
            if (httpClient == null) throw new ArgumentNullException("httpClient");
            if (logger == null) throw new ArgumentNullException("logger");

            _url = url;
            _timeout = timeout;
            _httpClient = httpClient;
            _logger = logger;

            _buffer = new byte[BufferSize];

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

        public void Download(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Downloading...");
                _logger.LogTrace("url = " + _url);
                _logger.LogTrace("bufferSize = " + BufferSize);
                _logger.LogTrace("bytesRange = " + (_bytesRange.HasValue
                                     ? _bytesRange.Value.Start + "-" + _bytesRange.Value.End
                                     : "(none)"));
                _logger.LogTrace("timeout = " + _timeout);

                Assert.MethodCalledOnlyOnce(ref _downloadHasBeenCalled, "Download");

                var request = new HttpGetRequest
                {
                    Address = new Uri(_url),
                    Range = _bytesRange,
                    Timeout = _timeout,
                    ReadWriteTimeout = _timeout,
                };

                using (var response = _httpClient.Get(request))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _logger.LogDebug("Received response from server.");
                    _logger.LogTrace("statusCode = " + response.StatusCode);

                    if (Is2XXStatus(response.StatusCode))
                    {
                        _logger.LogDebug("Successful response. Reading response stream...");

                        //TODO: Could response.ContentStream be null? Need to check it.

                        ReadResponseStream(response.ContentStream, cancellationToken);

                        _logger.LogDebug("Stream has been read.");
                    }
                    else if (Is4XXStatus(response.StatusCode))
                    {
                        throw new DataNotAvailableException(string.Format(
                            "Request data for {0} is not available (status: {1})", _url, response.StatusCode));
                    }
                    else
                    {
                        throw new ServerErrorException(string.Format(
                            "Server has experienced some issues with request for {0} which resulted in {1} status code.",
                            _url, response.StatusCode));
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

        private void ReadResponseStream(Stream responseStream, CancellationToken cancellationToken)
        {
            int bufferRead;
            while ((bufferRead = responseStream.Read(_buffer, 0, BufferSize)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                OnDataAvailable(_buffer, bufferRead);
            }
        }

        // ReSharper disable once InconsistentNaming
        private static bool Is2XXStatus(HttpStatusCode statusCode)
        {
            return (int) statusCode >= 200 && (int) statusCode <= 299;
        }

        // ReSharper disable once InconsistentNaming
        private static bool Is4XXStatus(HttpStatusCode statusCode)
        {
            return (int) statusCode >= 400 && (int) statusCode <= 499;
        }

        private void OnDataAvailable(byte[] data, int length)
        {
            var handler = DataAvailable;
            if (handler != null) handler(data, length);
        }
    }
}