using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Common;
using PatchKit.Async;
using Debug = UnityEngine.Debug;

namespace PatchKit.Unity.Web
{
    internal class TorrentDownloader
    {
        public readonly int Timeout;

        public TorrentDownloader(int timeout)
        {
            Timeout = timeout;
        }

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
                Debug.Log("Creating torrent engine.");
                
                using (var torrentManager = new TorrentManager(
                    Torrent.Load(torrentPath), downloadDir, new TorrentSettings()))
                {
                    Debug.Log("Creating torrent manager.");

                    engine.Register(torrentManager);

                    engine.StartAll();

                    Debug.Log("Starting all torrents.");

                    DateTime startTime = DateTime.Now;

                    Stopwatch stopwatch = new Stopwatch();

                    stopwatch.Start();

                    double lastProgress = 0.0;

                    while (!torrentManager.Complete)
                    {
                        if (torrentManager.Progress < 0.0001 && (DateTime.Now - startTime).TotalMilliseconds > Timeout)
                        {
                            throw new TimeoutException("Torrent timeout exception.");
                        }

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

                        if(torrentManager.State == TorrentState.Error)
                        {
                            throw new WebException("Torrent error.");
                        }

                        if(torrentManager.State == TorrentState.Paused || torrentManager.State == TorrentState.Stopped)
                        {
                            torrentManager.Start();
                        }

                        if (stopwatch.ElapsedMilliseconds > 1500 && torrentManager.Progress > lastProgress)
                        {
                            try
                            {
                                Debug.Log("Torrent status:" + "\n" +
                                      "Open connections - " + torrentManager.OpenConnections + "\n" +
                                      "Current tracker Uri - " + torrentManager.TrackerManager.CurrentTracker.Uri + "\n" +
                                      "Current tracker failure message - " + torrentManager.TrackerManager.CurrentTracker.FailureMessage + "\n" +
                                      "Current tracker warning message - " + torrentManager.TrackerManager.CurrentTracker.WarningMessage + "\n" +
                                      "Current tracker status - " + torrentManager.TrackerManager.CurrentTracker.Status + "\n" +
                                      "Current tracker complete - " + torrentManager.TrackerManager.CurrentTracker.Complete + "\n" +
                                      "Engine Peer Id - " + torrentManager.Engine.PeerId + "\n" +
                                      "Dht Engine State - " + torrentManager.Engine.DhtEngine.State + "\n" +
                                      "Inactive peers - " + torrentManager.InactivePeers + "\n" +
                                      "Hash fails" + torrentManager.HashFails);
                            }
                            catch (Exception)
                            {
                                // ignored
                            }

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