using System;
using System.Diagnostics;
using System.IO;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents
{
    public class UnityTorrentClientProcessStartInfoProvider : ITorrentClientProcessStartInfoProvider
    {
        public const string TorrentClientDirectory = "Helpers/p2p/";
        public const string TorrentClientFileName = "patcher-p2p-helper";

        private static string ResolveTorrentClientFileName()
        {
            return ResolveTorrentClientFileName(Platform.GetPlatformType());
        }

        public static string ResolveTorrentClientFileName(PlatformType platform)
        {
            if (platform == PlatformType.Windows)
            {
                return Path.Combine(TorrentClientDirectory, TorrentClientFileName + ".exe");
            }

            return Path.Combine(TorrentClientDirectory, TorrentClientFileName);
        }

        public ProcessStartInfo GetProcessStartInfo()
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = ResolveTorrentClientFileName(),
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (Platform.IsWindows())
            {
                return processStartInfo;
            }

            if (Platform.IsOSX())
            {
                // make sure that binary can be executed
                Chmod.SetExecutableFlag(processStartInfo.FileName);

                processStartInfo.EnvironmentVariables["DYLD_LIBRARY_PATH"] = TorrentClientDirectory;

                return processStartInfo;
            }

            if (Platform.IsLinux())
            {
                // make sure that binary can be executed
                Chmod.SetExecutableFlag(processStartInfo.FileName);

                return processStartInfo;
            }

            throw new UnsupportedPlatformException("Unsupported platform by torrent-client.");
        }
    }
}