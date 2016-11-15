using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Newtonsoft.Json.Linq;
using PatchKit.Async;
using Debug = UnityEngine.Debug;

namespace PatchKit.Unity.Web
{
    internal class TorrentDownloader
    {
        private readonly string _streamingAssetsPath;

        public TorrentDownloader(string streamingAssetsPath)
        {
            _streamingAssetsPath = streamingAssetsPath;
        }

        public void DownloadFile(string torrentPath, string destinationPath,
            DownloaderProgressHandler onDownloaderProgress, AsyncCancellationToken cancellationToken)
        {
            using (var torrentClient = new TorrentClient(_streamingAssetsPath))
            {
                string destinationDirectoryPath = destinationPath + "_dir";

                var addTorrentResult =
                    torrentClient.ExecuteCommand(string.Format("add-torrent {0} {1}", torrentPath,
                        destinationDirectoryPath));

                if (addTorrentResult.Value<string>("status") != "ok")
                {
                    throw new Exception("Wrong torrent-client status - " + addTorrentResult.Value<string>("status"));
                }

                bool downloaded = false;

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                double lastProgress = 0.0;

                while (!downloaded)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var statusResult = torrentClient.ExecuteCommand("status");

                    Debug.Log("Torrent status:\n" + statusResult);

                    if (statusResult.Value<string>("status") != "ok")
                    {
                        throw new Exception("Wrong torrent-client status - " + statusResult.Value<string>("status"));
                    }

                    if (statusResult["data"].Value<int>("count") < 1)
                    {
                        throw new Exception("Couldn't check status of torrent-client.");
                    }

                    var torrentStatus = statusResult["data"].Value<JArray>("torrents")[0];

                    if (torrentStatus.Value<string>("error") != string.Empty)
                    {
                        throw new Exception("torrent-client error: " + torrentStatus.Value<string>("error"));
                    }

                    if (torrentStatus.Value<bool>("is_seeding"))
                    {
                        downloaded = true;
                    }

                    double progress = torrentStatus.Value<double>("progress");
                    long totalBytes = torrentStatus.Value<long>("total_wanted");
                    long bytes = (long) (progress*totalBytes);
                    float speed = CalculateDownloadSpeed(progress, lastProgress, totalBytes,
                        stopwatch.ElapsedMilliseconds);

                    stopwatch.Reset();
                    stopwatch.Start();
                    lastProgress = progress;

                    onDownloaderProgress((float) progress, speed, bytes, totalBytes);

                    Thread.Sleep(1000);
                }

                var dirInfo = new DirectoryInfo(destinationDirectoryPath);

                File.Move(dirInfo.GetFiles()[0].FullName, destinationPath);

                Directory.Delete(destinationDirectoryPath, true);
            }
        }

        private static float CalculateDownloadSpeed(double progress, double lastProgress, long totalBytes, long elapsedMilliseconds)
        {
            if (elapsedMilliseconds == 0)
            {
                return 0.0f;
            }

            double elapsedSeconds = elapsedMilliseconds/1000.0f;

            double progressDelta = progress - lastProgress;

            double bytes = progressDelta * totalBytes;

            return (float)(bytes / 1024.0 / elapsedSeconds);
        }
    }
}