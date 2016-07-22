using System.Diagnostics;
using System.Net;
using System.Threading;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;
using PatchKit.Async;

namespace PatchKit.Unity.Web
{
    internal class TorrentDownloader
    {
        public void DownloadFile(string torrentPath, string destinationPath, DownloaderProgressHandler onDownloaderProgress, AsyncCancellationToken cancellationToken)
        {
            string downloadDir = destinationPath + "_data";

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
                    Torrent.Load(torrentPath), downloadDir, new TorrentSettings()))
                {
                    engine.Register(torrentManager);

                    engine.StartAll();

                    Stopwatch stopwatch = new Stopwatch();

                    stopwatch.Start();

                    double lastProgress = 0.0;

                    while (!torrentManager.Complete)
                    {
                        if (cancellationToken.IsCancellationRequested)
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

                        if (stopwatch.ElapsedMilliseconds > 3000 && torrentManager.Progress > lastProgress)
                        {
                            var downloadSpeed = CalculateDownloadSpeed(torrentManager.Progress, lastProgress,
                                torrentManager.Torrent.Size, stopwatch.ElapsedMilliseconds);

                            onDownloaderProgress((float)torrentManager.Progress / 100.0f, downloadSpeed, (long) (torrentManager.Torrent.Size * torrentManager.Progress / 100.0), torrentManager.Torrent.Size);

                            lastProgress = torrentManager.Progress;

                            stopwatch.Reset();
                            stopwatch.Start();
                        }

                        Thread.Sleep(100);
                    }

                    onDownloaderProgress(1.0f, 0.0f, torrentManager.Torrent.Size, torrentManager.Torrent.Size);

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
            System.IO.File.Move(downloadTorrentFile, destinationPath);
        }

        private static float CalculateDownloadSpeed(double progress, double lastProgress, long totalBytes, long elapsedMilliseconds)
        {
            if (elapsedMilliseconds == 0)
            {
                return 0.0f;
            }

            double elapsedSeconds = elapsedMilliseconds/1000.0f;

            double progressDelta = (progress - lastProgress) / 100.0;

            double bytes = progressDelta * totalBytes;

            return (float)(bytes / 1024.0 / elapsedSeconds);
        }
    }
}