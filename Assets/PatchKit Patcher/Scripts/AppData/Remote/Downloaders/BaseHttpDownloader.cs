using System;
using System.IO;
using System.Net;
using JetBrains.Annotations;
using PatchKit.Logging;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public sealed class BaseHttpDownloader : IBaseHttpDownloader
    {
        private readonly ILogger _logger;

        private const int BufferSize = 1024;

        private readonly IHttpClient _httpClient;

        public BaseHttpDownloader([NotNull] IHttpClient httpClient, [NotNull] ILogger logger)
        {
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }

            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            _httpClient = httpClient;
            _logger = logger;

            ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, errors) => true;
            ServicePointManager.DefaultConnectionLimit = 65535;
        }

        public void Download(string url, BytesRange? bytesRange, int timeout, DataAvailableHandler onDataAvailable,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Value cannot be null or empty.", "url");
            }

            if (timeout <= 0)
            {
                throw new ArgumentOutOfRangeException("timeout");
            }

            try
            {
                _logger.LogDebug("Downloading...");
                _logger.LogTrace("url = " + url);
                _logger.LogTrace("bytesRange = " + (bytesRange.HasValue
                                     ? bytesRange.Value.Start + "-" + bytesRange.Value.End
                                     : "(none)"));
                _logger.LogTrace("timeout = " + timeout);
                _logger.LogTrace("bufferSize = " + BufferSize);

                var request = new HttpGetRequest
                {
                    Address = new Uri(url),
                    Range = bytesRange,
                    Timeout = timeout
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

                        ReadResponseStream(response.ContentStream, onDataAvailable, cancellationToken);

                        _logger.LogDebug("Stream has been read.");
                    }
                    else if (Is4XXStatus(response.StatusCode))
                    {
                        throw new DataNotAvailableException(string.Format(
                            "Request data for {0} is not available (status: {1})", url, response.StatusCode));
                    }
                    else
                    {
                        throw new ServerErrorException(string.Format(
                            "Server has experienced some issues with request for {0} which resulted in {1} status code.",
                            url, response.StatusCode));
                    }
                }

                _logger.LogDebug("Downloading finished.");
            }
            catch (WebException webException)
            {
                _logger.LogError("Downloading has failed.", webException);
                throw new ConnectionFailureException(
                    string.Format("Connection to server has failed while requesting {0}", url), webException);
            }
            catch (Exception e)
            {
                _logger.LogError("Downloading has failed.", e);
                throw;
            }
        }

        private void ReadResponseStream(Stream responseStream, DataAvailableHandler onDataAvailable, CancellationToken cancellationToken)
        {
            var buffer = new byte[BufferSize];
            int bufferRead;
            while ((bufferRead = responseStream.Read(buffer, 0, BufferSize)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                onDataAvailable(buffer, bufferRead);
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
    }
}