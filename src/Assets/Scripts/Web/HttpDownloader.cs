using System;
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

        public bool DownloadFile(string[] urls, string destinationPath, long totalDownloadBytesCount,
            DownloaderProgressHandler onDownloadProgress, AsyncCancellationToken cancellationToken)
        {
            bool atLeastOneWorking;
            do
            {
                atLeastOneWorking = false;

                foreach (var url in urls)
                {
                    UnityEngine.Debug.Log(string.Format("Starting HTTP download of content package file from {0} to {1}.", url,
                            destinationPath));

                    try
                    {
                        long offset = 0;
                        if (File.Exists(destinationPath))
                        {
                            offset = new FileInfo(destinationPath).Length;
                            UnityEngine.Debug.Log("Current file size: " + offset);
                        }

                        DownloadFile(url, destinationPath, totalDownloadBytesCount, offset, totalDownloadBytesCount,
                            onDownloadProgress, cancellationToken);

                        if (File.Exists(destinationPath))
                        {
                            long newFileSize = new FileInfo(destinationPath).Length;

                            if (newFileSize == totalDownloadBytesCount)
                            {
                                UnityEngine.Debug.Log("Downloaded all of it!");
                                return true;
                            }

                            UnityEngine.Debug.Log("Contant is still not complete...");
                            if (newFileSize != offset)
                            {
                                atLeastOneWorking = true;
                            }

                            UnityEngine.Debug.Log("Will try again in 10 seconds...");
                            Thread.Sleep(10000);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogException(e);

                        UnityEngine.Debug.Log("Will try again in 10 seconds...");
                        Thread.Sleep(10000);
                    }
                }
            } while (atLeastOneWorking);

            // all mirrors down!
            return false;
        }

        public bool DownloadFile(string url, string destinationPath, long totalDownloadBytesCount, long offset, long contentLength,
            DownloaderProgressHandler onDownloadProgress, AsyncCancellationToken cancellationToken)
        {
            onDownloadProgress(CalculateProgress(offset, totalDownloadBytesCount), 0.0f, offset, totalDownloadBytesCount);

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

                onDownloadProgress(CalculateProgress(totalReadBytesCount, totalDownloadBytesCount),
                    0.0f, totalDownloadBytesCount, totalDownloadBytesCount);
                return true;
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
