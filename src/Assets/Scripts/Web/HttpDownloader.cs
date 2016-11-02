using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using PatchKit.Async;

namespace PatchKit.Unity.Web
{
    internal class HttpDownloader
    {
        public HttpDownloader()
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
        }

        public void DownloadFile(string url, string destinationPath, long totalDownloadBytesCount, long offset, long contentLength,
            DownloaderProgressHandler onDownloadProgress, AsyncCancellationToken cancellationToken)
        {
            onDownloadProgress(0.0f, 0.0f, 0, totalDownloadBytesCount);

            ServicePointManager.DefaultConnectionLimit = 65535;

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = 10000;
            request.AddRange((int) offset); // TODO: http://stackoverflow.com/questions/6576397/how-to-specify-range-2gb-for-httpwebrequest-in-net-3-5

            using (var response = request.GetResponse())
            {
                if (contentLength == 0)
                {
                    contentLength = response.ContentLength;
                }
                
                long totalReadBytesCount = offset;
                cancellationToken.ThrowIfCancellationRequested();

                UnityEngine.Debug.Log("http content length: " + response.ContentLength);

                using (var responseStream = response.GetResponseStream())
                {
                    if (responseStream == null)
                    {
                        throw new WebException("Empty response stream.");
                    }

                    const int downloaderBufferSize = 1024;

                    byte[] buffer = new byte[downloaderBufferSize];

                    Stopwatch stopwatch = new Stopwatch();

                    stopwatch.Start();

                    long lastTotalReadBytesCount = 0;

                    using (
                        var destinationStream = new FileStream(destinationPath, FileMode.OpenOrCreate, FileAccess.Write,
                            FileShare.None))
                    {
                        if (totalReadBytesCount != 0)
                        {
                            destinationStream.Seek(0, SeekOrigin.End);
                        }

                        int readBytesCount;

                        while ((readBytesCount = responseStream.Read(buffer, 0, downloaderBufferSize)) > 0)
                        {
                            totalReadBytesCount += readBytesCount;

                            cancellationToken.ThrowIfCancellationRequested();
                            destinationStream.Write(buffer, 0, readBytesCount);

                            if (stopwatch.ElapsedMilliseconds > 1500)
                            {
                                UnityEngine.Debug.Log("Bytes downloaded: " + totalReadBytesCount);

                                onDownloadProgress(CalculateProgress(totalReadBytesCount, totalDownloadBytesCount),
                                    CalculateDownloadSpeed(totalReadBytesCount - lastTotalReadBytesCount, stopwatch.ElapsedMilliseconds),
                                    totalReadBytesCount, totalDownloadBytesCount);

                                lastTotalReadBytesCount = totalReadBytesCount;

                                stopwatch.Reset();
                                stopwatch.Start();
                            }
                        }
                    }

                }

                if (totalReadBytesCount != contentLength)
                {
                    UnityEngine.Debug.LogError("Downloaded content length is different: " + totalReadBytesCount);
                    UnityEngine.Debug.Log("Will try again in 10 seconds...");
                    Thread.Sleep(10000);

                    DownloadFile(url, destinationPath, totalDownloadBytesCount,
                        totalReadBytesCount, contentLength, onDownloadProgress, cancellationToken);
                } else
                {
                    onDownloadProgress(1.0f, 0.0f, totalDownloadBytesCount, totalDownloadBytesCount);
                }
            }
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
