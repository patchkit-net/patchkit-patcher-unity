using System;
using System.IO;
using System.Net;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.Data.Remote.Downloaders
{
    /// <summary>
    /// Base HTTP downloader. Data is transfered through events.
    /// </summary>
    internal sealed class BaseHttpDownloader
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(BaseHttpDownloader));

        private readonly string _url;
        private readonly int _bufferSize;
        private readonly byte[] _buffer;
        private readonly int _timeout;

        private HttpWebRequest _request;
        private bool _started;

        /// <summary>
        /// Occurs when request is created. Could be used to make some adjustements to request.
        /// </summary>
        public event RequestCreatedHandler RequestCreated;

        /// <summary>
        /// Occurs when data is downloaded.
        /// </summary>
        public event DataDownloadedHandler DataDownloaded;

        public BaseHttpDownloader(string url, int timeout, int bufferSize = 1024)
        {
            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(url, "url");
            DebugLogger.LogVariable(timeout, "timeout");
            DebugLogger.LogVariable(bufferSize, "bufferSize");

            Checks.ArgumentNotNullOrEmpty(url, "url");
            Checks.ArgumentMoreThanZero(timeout, "timeout");
            Checks.ArgumentMoreThanZero(bufferSize, "bufferSize");

            _url = url;
            _timeout = timeout;
            _bufferSize = bufferSize;
            _buffer = new byte[_bufferSize];

            ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, errors) => true;
            ServicePointManager.DefaultConnectionLimit = 65535;
        }

        private void CreateRequest()
        {
            DebugLogger.Log("Creating request");

            _request = (HttpWebRequest)WebRequest.Create(_url);
            _request.Method = "GET";
            _request.Timeout = _timeout;

            OnRequestCreated(_request);
        }

        private void VerifyResponse(HttpWebResponse response)
        {
            DebugLogger.Log("Veryfing response");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new DownloaderException("Resource not found - " + _url, DownloaderExceptionStatus.NotFound);
            }

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.PartialContent)
            {
                throw new DownloaderException("Resource request returned status code " + response.StatusCode + " - " + _url, DownloaderExceptionStatus.Other);
            }
        }

        private void ProcessResponse(HttpWebResponse response, CancellationToken cancellationToken)
        {
            DebugLogger.Log("Processing response");

            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null)
                {
                    throw new DownloaderException("Resource response stream is null - " + _url, DownloaderExceptionStatus.EmptyStream);
                }

                ProcessStream(responseStream, cancellationToken);
            }
        }

        private void ProcessStream(Stream responseStream, CancellationToken cancellationToken)
        {
            DebugLogger.Log("Processing stream");

            int bufferRead;
            while ((bufferRead = responseStream.Read(_buffer, 0, _bufferSize)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                OnDataDownloaded(_buffer, bufferRead);
            }
        }

        public void Download(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Starting download.");

            if (_started)
            {
                throw new InvalidOperationException("Cannot start the same BaseHttpDownloader twice.");
            }
            _started = true;

            CreateRequest();

            cancellationToken.ThrowIfCancellationRequested();

            DebugLogger.Log("Retrieving response from request");

            using (var response = _request.GetResponse())
            {
                VerifyResponse((HttpWebResponse) response);
                ProcessResponse((HttpWebResponse) response, cancellationToken);
            }
        }

        private void OnRequestCreated(HttpWebRequest request)
        {
            if (RequestCreated != null) RequestCreated(request);
        }

        private void OnDataDownloaded(byte[] bytes, int length)
        {
            if (DataDownloaded != null) DataDownloaded(bytes, length);
        }
    }
}
