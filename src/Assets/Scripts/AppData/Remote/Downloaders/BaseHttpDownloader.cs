using System.IO;
using System.Net;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public sealed class BaseHttpDownloader
    {
        public delegate IHttpWebRequestAdapter CreateNewHttpWebRequest(string url);

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(BaseHttpDownloader));

        private readonly string _url;
        private readonly int _bufferSize;
        private readonly CreateNewHttpWebRequest _createNewHttpWebRequest;
        private readonly byte[] _buffer;
        private readonly int _timeout;

        private IHttpWebRequestAdapter _request;

        private bool _downloadHasBeenCalled;

        public event RequestCreatedHandler RequestCreated;

        public event DataDownloadedHandler DataAvailable;

        public BaseHttpDownloader(string url, int timeout, int bufferSize = 1024) : 
            this(url, timeout, bufferSize, CreateDefaultHttpWebRequest)
        {
        }

        public BaseHttpDownloader(string url, int timeout, int bufferSize,
            CreateNewHttpWebRequest createNewHttpWebRequest)
        {
            Checks.ArgumentNotNullOrEmpty(url, "url");
            Checks.ArgumentMoreThanZero(timeout, "timeout");
            Checks.ArgumentMoreThanZero(bufferSize, "bufferSize");
            AssertChecks.ArgumentNotNull(createNewHttpWebRequest, "createNewHttpWebRequest");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(url, "url");
            DebugLogger.LogVariable(timeout, "timeout");
            DebugLogger.LogVariable(bufferSize, "bufferSize");

            _url = url;
            _timeout = timeout;
            _bufferSize = bufferSize;
            _createNewHttpWebRequest = createNewHttpWebRequest;
            _buffer = new byte[_bufferSize];

            ServicePointManager.ServerCertificateValidationCallback =
                (sender, certificate, chain, errors) => true;
            ServicePointManager.DefaultConnectionLimit = 65535;
        }

        private void CreateRequest()
        {
            DebugLogger.Log("Creating request");

            _request = _createNewHttpWebRequest(_url);
            _request.Method = "GET";
            _request.Timeout = _timeout;

            OnRequestCreated(_request);
        }

        private void VerifyResponse(IHttpWebResponseAdapter response)
        {
            DebugLogger.Log("Veryfing response");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new DownloaderException("Resource not found - " + _url, DownloaderExceptionStatus.NotFound);
            }

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.PartialContent)
            {
                throw new DownloaderException(
                    "Resource request returned status code " + response.StatusCode + " - " + _url,
                    DownloaderExceptionStatus.Other);
            }
        }

        private void ProcessResponse(IHttpWebResponseAdapter response, CancellationToken cancellationToken)
        {
            DebugLogger.Log("Processing response");

            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null)
                {
                    throw new DownloaderException("Resource response stream is null - " + _url,
                        DownloaderExceptionStatus.EmptyStream);
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

                OnDataAvailable(_buffer, bufferRead);
            }
        }

        public void Download(CancellationToken cancellationToken)
        {
            AssertChecks.MethodCalledOnlyOnce(ref _downloadHasBeenCalled, "Download");

            DebugLogger.Log("Starting download.");

            CreateRequest();

            cancellationToken.ThrowIfCancellationRequested();

            DebugLogger.Log("Retrieving response from request");

            using (var response = _request.GetResponse())
            {
                VerifyResponse(response);
                ProcessResponse(response, cancellationToken);
            }
        }

        private void OnRequestCreated(IHttpWebRequestAdapter request)
        {
            if (RequestCreated != null) RequestCreated(request);
        }

        private void OnDataAvailable(byte[] bytes, int length)
        {
            if (DataAvailable != null) DataAvailable(bytes, length);
        }

        private static IHttpWebRequestAdapter CreateDefaultHttpWebRequest(string url)
        {
            return new HttpWebRequestAdapter((HttpWebRequest)WebRequest.Create(url));
        }
    }
}