using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Statistics;
using Debug = UnityEngine.Debug;

namespace PatchKit.Unity.Patcher.Net
{
    internal class TorrentDownloader
    {
        private readonly string _streamingAssetsPath;

        private readonly long _timeout;

        private struct DownloadSpeed
        {
            public long Bytes;

            public long Time;

            public DateTime AddTime;
        }

        public TorrentDownloader(string streamingAssetsPath, long timeout)
        {
            _streamingAssetsPath = streamingAssetsPath;
            _timeout = timeout;
        }

        public void DownloadFile(string torrentPath, string destinationPath,
            CustomProgressReporter<DownloadProgress> progressReporter, CancellationToken cancellationToken)
        {
            using (var torrentClient = new TorrentClient(_streamingAssetsPath))
            {
                string destinationDirectoryPath = destinationPath + "_dir";

                string specialTorrentPath = torrentPath.Replace("\\", "/").Replace(" ", "\\ ");

                string specialDestinationDirectoryPath = destinationDirectoryPath.Replace("\\", "/").Replace(" ", "\\ ");

                var addTorrentResult =
                    torrentClient.ExecuteCommand(string.Format("add-torrent {0} {1}", specialTorrentPath,
                        specialDestinationDirectoryPath));

                Debug.Log("Add torrent:\n" + addTorrentResult);

                if (addTorrentResult.Value<string>("status") != "ok")
                {
                    throw new Exception("Wrong torrent-client status - " + addTorrentResult.Value<string>("status"));
                }

                bool downloaded = false;

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Stopwatch timeoutWatch = new Stopwatch();
                timeoutWatch.Start();

                List<DownloadSpeed> downloadSpeedList = new List<DownloadSpeed>();

                long lastBytes = 0;

                while (!downloaded)
                {
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
                    long bytes = (long)(progress * totalBytes);

                    downloadSpeedList.Add(new DownloadSpeed
                    {
                        Bytes = bytes - lastBytes,
                        Time = stopwatch.ElapsedMilliseconds,
                        AddTime = DateTime.Now
                    });

                    if (bytes != lastBytes)
                    {
                        timeoutWatch.Reset();
                        timeoutWatch.Start();
                    }
                    else if (timeoutWatch.ElapsedMilliseconds > _timeout)
                    {
                        throw new TimeoutException("Downloading torrent has timed out.");
                    }

                    lastBytes = bytes;

                    downloadSpeedList.RemoveAll(s => (DateTime.Now - s.AddTime).Seconds > 10);

                    float speed = CalculateDownloadSpeed(downloadSpeedList.Sum(s => s.Bytes),
                        downloadSpeedList.Sum(s => s.Time));

                    stopwatch.Reset();
                    stopwatch.Start();

                    progressReporter.Progress = new DownloadProgress
                    {
                        DownloadedBytes = bytes,
                        TotalBytes = totalBytes, 
                        KilobytesPerSecond = speed,
                        Progress = progress
                    };

                    Thread.Sleep(1000);
                }

                var dirInfo = new DirectoryInfo(destinationDirectoryPath);

                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }

                File.Move(dirInfo.GetFiles()[0].FullName, destinationPath);

                Directory.Delete(destinationDirectoryPath, true);
            }
        }

        private static float CalculateDownloadSpeed(long bytes, long time)
        {
            if (bytes == 0)
            {
                return 0.0f;
            }

            double elapsedSeconds = time / 1000.0f;

            return (float)(bytes / 1024.0 / elapsedSeconds);
        }
    }
}