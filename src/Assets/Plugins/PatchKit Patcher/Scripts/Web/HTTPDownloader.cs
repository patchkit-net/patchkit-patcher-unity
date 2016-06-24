using System.Diagnostics;
using System.IO;
using System.Net;
using PatchKit.API.Async;

namespace PatchKit.Unity.Patcher.Web
{
    internal class HttpDownloader
    {
        public HttpDownloader()
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        }

        public delegate void DownloadProgressHandler(float progress, float speed);

        public void DownloadFile(string url, string destinationPath, long totalDownloadBytesCount,
            DownloadProgressHandler onDownloadProgress, AsyncCancellationToken cancellationToken)
        {
            onDownloadProgress(0.0f, 0.0f);

            ServicePointManager.DefaultConnectionLimit = 65535;

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = 10000;

            using (var response = request.GetResponse())
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream == null)
                    {
                        throw new WebException("Empty response stream.");
                    }

                    const int downloaderBufferSize = 1024;

                    byte[] buffer = new byte[downloaderBufferSize];

                    long totalReadBytesCount = 0;

                    Stopwatch stopwatch = new Stopwatch();

                    stopwatch.Start();

                    using (
                        var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write,
                            FileShare.None))
                    {
                        int readBytesCount;

                        while ((readBytesCount = responseStream.Read(buffer, 0, downloaderBufferSize)) > 0)
                        {
                            totalReadBytesCount += readBytesCount;

                            onDownloadProgress(CalculateProgress(totalReadBytesCount, totalDownloadBytesCount),
                                CalculateDownloadSpeed(readBytesCount, stopwatch.ElapsedMilliseconds));

                            cancellationToken.ThrowIfCancellationRequested();
                            destinationStream.Write(buffer, 0, readBytesCount);

                            stopwatch.Reset();
                            stopwatch.Start();
                        }
                    }
                }
            }

            onDownloadProgress(1.0f, 0.0f);
        }

        private float CalculateProgress(long bytes, long totalBytes)
        {
            if (totalBytes == 0)
            {
                return 0.0f;
            }

            return (float)bytes/totalBytes;
        }

        private float CalculateDownloadSpeed(long bytes, long elapsedMilliseconds)
        {
            if (elapsedMilliseconds == 0)
            {
                return 0.0f;
            }

            return (float) (bytes/1024.0/(elapsedMilliseconds/1000.0));
        }
    }
}