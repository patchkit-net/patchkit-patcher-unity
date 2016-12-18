using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Statistics;
using Debug = UnityEngine.Debug;

namespace PatchKit.Unity.Patcher.Net
{
    internal class HttpDownloader
    {
        private readonly int _timeout;

        private const int DownloadBufferSize = 1024;

        private struct DownloadSpeed
        {
            public long Bytes;

            public long Time;

            public DateTime AddTime;
        }

        public HttpDownloader(int timeout = 10000)
        {
            _timeout = timeout;
            ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            ServicePointManager.DefaultConnectionLimit = 65535;
        }

        private WebRequest CreateRequest(string url)
        {
            Debug.Log(string.Format("Creating a request for url {0}", url));

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = _timeout;

            return request;
        }

        private void DownloadStream(Stream responseStream, Stream destinationFileStream, long totalBytes,
            CustomProgressReporter<DownloadProgress> progressReporter, CancellationToken cancellationToken)
        {
            Debug.Log("Downloading data from response stream.");

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            byte[] buffer = new byte[DownloadBufferSize];
            int bufferRead;

            long downloadedBytes = 0;

            List<DownloadSpeed> downloadSpeedList = new List<DownloadSpeed>();

            while ((bufferRead = responseStream.Read(buffer, 0, DownloadBufferSize)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                destinationFileStream.Write(buffer, 0, bufferRead);
                downloadedBytes += bufferRead;

                downloadSpeedList.Add(new DownloadSpeed
                {
                    Bytes = bufferRead,
                    Time = stopwatch.ElapsedMilliseconds,
                    AddTime = DateTime.Now
                });

                downloadSpeedList.RemoveAll(s => (DateTime.Now - s.AddTime).Seconds > 10);

                stopwatch.Reset();
                stopwatch.Start();

                progressReporter.Progress = new DownloadProgress
                {
                    DownloadedBytes = downloadedBytes,
                    TotalBytes = totalBytes,
                    KilobytesPerSecond = CalculateDownloadSpeed(downloadSpeedList.Sum(s => s.Bytes), downloadSpeedList.Sum(s => s.Time)),
                    Progress = totalBytes == 0 ? 0 : downloadedBytes/(double) totalBytes
                };
            }
        }

        private void ProcessResponse(WebResponse response, string destinationFilePath, long totalBytes,
            CustomProgressReporter<DownloadProgress> progressReporter, CancellationToken cancellationToken)
        {
            Debug.Log("Processing web response.");

            using (var responseStream = response.GetResponseStream())
            {
                if (responseStream == null)
                {
                    throw new WebException("Null response stream.");
                }

                using (
                    var destinationFileStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write,
                        FileShare.None))
                {
                    DownloadStream(responseStream, destinationFileStream, totalBytes, progressReporter,
                        cancellationToken);
                }
            }
        }

        public void DownloadFile(string url, string destinationFilePath, long totalBytes,
            CustomProgressReporter<DownloadProgress> progressReporter,
            CancellationToken cancellationToken)
        {
            Debug.Log(string.Format("Downloading file from {0} to {1}", url, destinationFilePath));

            progressReporter.Progress = new DownloadProgress
            {
                DownloadedBytes = 0,
                TotalBytes = totalBytes,
                KilobytesPerSecond = 0,
                Progress = 0.0
            };

            var request = CreateRequest(url);

            using (var response = request.GetResponse())
            {
                ProcessResponse(response, destinationFilePath, totalBytes, progressReporter, cancellationToken);
            }

            progressReporter.Progress = new DownloadProgress
            {
                DownloadedBytes = totalBytes,
                TotalBytes = totalBytes,
                KilobytesPerSecond = 0,
                Progress = 1.0
            };
        }

        private static double CalculateDownloadSpeed(long bytes, long elapsedMilliseconds)
        {
            if (elapsedMilliseconds == 0)
            {
                return 0.0f;
            }

            return (float) (bytes/1024.0/(elapsedMilliseconds/1000.0));
        }
    }
}