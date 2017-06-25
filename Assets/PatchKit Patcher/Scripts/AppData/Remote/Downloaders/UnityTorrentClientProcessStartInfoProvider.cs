using System;
using System.Diagnostics;
using System.IO;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class UnityTorrentClientProcessStartInfoProvider : ITorrentClientProcessStartInfoProvider
    {
        private const string TorrentClientWinPath = "torrent-client/win/torrent-client.exe";
        private const string TorrentClientOsx64Path = "torrent-client/osx64/torrent-client";
        private const string TorrentClientLinux64Path = "torrent-client/linux64/torrent-client";

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

                processStartInfo.EnvironmentVariables["DYLD_LIBRARY_PATH"] = Path.Combine(_streamingAssetsPath, "torrent-client/osx64");

                return processStartInfo;
            }

            if (Platform.IsLinux() && IntPtr.Size == 8) // Linux 64 bit
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = _streamingAssetsPath.PathCombine(TorrentClientLinux64Path),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // make sure that binary can be executed
                Chmod.SetExecutableFlag(processStartInfo.FileName);

                processStartInfo.EnvironmentVariables["LD_LIBRARY_PATH"] = Path.Combine(_streamingAssetsPath, "torrent-client/linux64");

                return processStartInfo;
            }

            throw new TorrentClientException("Unsupported platform by torrent-client.");
        }
    }
}
