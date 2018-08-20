using System;
using System.Diagnostics;
using System.IO;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents
{
    public class UnityTorrentClientProcessStartInfoProvider : ITorrentClientProcessStartInfoProvider
    {
        private const string TorrentClientDirectory = "patcher-p2p-helper";
        private const string TorrentClientFileName = "patcher-p2p-helper";

        private static readonly string TorrentClientWinPath = string.Format("{0}/win/{1}.exe", TorrentClientDirectory, TorrentClientFileName);
        private static readonly string TorrentClientOsx64Path = string.Format("{0}/osx64/{1}", TorrentClientDirectory, TorrentClientFileName);
        private static readonly string TorrentClientLinux64Path = string.Format("{0}/linux64/{1}", TorrentClientDirectory, TorrentClientFileName);
        private static readonly string TorrentClientLinux32Path = string.Format("{0}/linux32/{1}", TorrentClientDirectory, TorrentClientFileName);

        private string _streamingAssetsPath;

        public UnityTorrentClientProcessStartInfoProvider()
        {
            UnityDispatcher.Invoke(() =>
            {
                _streamingAssetsPath = Application.streamingAssetsPath;
            }).WaitOne();
        }

        public ProcessStartInfo GetProcessStartInfo()
        {
            if (Platform.IsWindows())
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _streamingAssetsPath.PathCombine(TorrentClientWinPath),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                return processStartInfo;
            }

            if (Platform.IsOSX())
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _streamingAssetsPath.PathCombine(TorrentClientOsx64Path),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // make sure that binary can be executed
                Chmod.SetExecutableFlag(processStartInfo.FileName);

                processStartInfo.EnvironmentVariables["DYLD_LIBRARY_PATH"] = Path.Combine(_streamingAssetsPath, Path.Combine(TorrentClientDirectory, "osx64"));

                return processStartInfo;
            }

            if (Platform.IsLinux()) // Linux 64 bit
            {
                string torrentClientPath = IntPtr.Size == 8 ? TorrentClientLinux64Path : TorrentClientLinux32Path;
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _streamingAssetsPath.PathCombine(torrentClientPath),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // make sure that binary can be executed
                Chmod.SetExecutableFlag(processStartInfo.FileName);

                return processStartInfo;
            }

            throw new UnsupportedPlatformException("Unsupported platform by torrent-client.");
        }
    }
}