using System.Diagnostics;
using System.Net;
using System.Threading;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Statistics;
using Debug = UnityEngine.Debug;

namespace PatchKit.Unity.Patcher.Net
{
    internal class TorrentDownloader
    {
        public void DownloadFile(string torrentFilePath, string destinationFilePath,
            CustomProgressReporter<DownloadProgress> progressReporter, CancellationToken cancellationToken)
        {
            Debug.Log(string.Format("Downloading file with torrent from {0} to {1}", torrentFilePath,
                destinationFilePath));

            progressReporter.Progress = new DownloadProgress
            {
                DownloadedBytes = 0,
                TotalBytes = 0,
                KilobytesPerSecond = 0,
                Progress = 0.0
            };

            string downloadDir = destinationFilePath + "_data";

            var settings = new EngineSettings
            {
                AllowedEncryption = EncryptionTypes.All,
                PreferEncryption = true,
                SavePath = downloadDir
            };

            string downloadTorrentFile;

            using (var engine = new ClientEngine(settings))
            {
                using (var torrentManager = new TorrentManager(
                    Torrent.Load(torrentFilePath), downloadDir, new TorrentSettings()))
                {
                    progressReporter.Progress = new DownloadProgress
                    {
                        DownloadedBytes = 0,
                        TotalBytes = torrentManager.Torrent.Size,
                        KilobytesPerSecond = 0,
                        Progress = 0.0
                    };

                    engine.Register(torrentManager);

                    engine.StartAll();

                    Stopwatch stopwatch = new Stopwatch();

                    stopwatch.Start();

                    double lastProgress = 0.0;

                    while (!torrentManager.Complete)
                    {
                        if (cancellationToken.IsCancelled)
                        {
                            torrentManager.Stop();
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        if (torrentManager.Error != null)
                        {
                            torrentManager.Stop();

                            throw new WebException(torrentManager.Error.Reason.ToString(),
                                torrentManager.Error.Exception);
                        }

                        if (torrentManager.State == TorrentState.Error)
                        {
                            throw new WebException("Torrent error.");
                        }

                        if (torrentManager.State == TorrentState.Paused || torrentManager.State == TorrentState.Stopped)
                        {
                            torrentManager.Start();
                        }

                        if (stopwatch.ElapsedMilliseconds > 3000 && torrentManager.Progress > lastProgress)
                        {
                            var downloadSpeed = CalculateDownloadSpeed(torrentManager.Progress, lastProgress,
                                torrentManager.Torrent.Size, stopwatch.ElapsedMilliseconds);

                            var downloadedBytes = (long) (torrentManager.Torrent.Size*torrentManager.Progress/100.0);

                            progressReporter.Progress = new DownloadProgress
                            {
                                DownloadedBytes = downloadedBytes,
                                TotalBytes = torrentManager.Torrent.Size,
                                KilobytesPerSecond = downloadSpeed,
                                Progress = downloadedBytes/(double) torrentManager.Torrent.Size
                            };

                            lastProgress = torrentManager.Progress;

                            stopwatch.Reset();
                            stopwatch.Start();
                        }

                        Thread.Sleep(100);
                    }

                    progressReporter.Progress = new DownloadProgress
                    {
                        DownloadedBytes = torrentManager.Torrent.Size,
                        TotalBytes = torrentManager.Torrent.Size,
                        KilobytesPerSecond = 0,
                        Progress = 1.0
                    };

                    TorrentFile[] files = torrentManager.Torrent.Files;
                    if (files.Length > 0)
                    {
                        downloadTorrentFile = files[0].FullPath;
                    }
                    else
                    {
                        throw new TorrentException("Missing files in downloaded torrent.");
                    }
                }
            }
            System.IO.File.Move(downloadTorrentFile, destinationFilePath);
        }

        private static float CalculateDownloadSpeed(double progress, double lastProgress, long totalBytes,
            long elapsedMilliseconds)
        {
            if (elapsedMilliseconds == 0)
            {
                return 0.0f;
            }

            double elapsedSeconds = elapsedMilliseconds/1000.0f;

            double progressDelta = (progress - lastProgress)/100.0;

            double bytes = progressDelta*totalBytes;

            return (float) (bytes/1024.0/elapsedSeconds);
        }
    }
}