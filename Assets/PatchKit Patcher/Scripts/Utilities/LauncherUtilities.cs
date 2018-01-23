using System;
using System.Diagnostics;
using System.IO;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Utilities
{
    public static class LauncherUtilities
    {
        private static readonly ILogger Logger = PatcherLogManager.DefaultLogger;

        private const string LauncherPathFileName = "launcher_path";

        private static string GetDefaultLauncherName(PlatformType platformType)
        {
            switch (platformType)
            {
                case PlatformType.Linux:
                    return "Launcher";
                case PlatformType.Windows:
                    return "Launcher.exe";
                case PlatformType.OSX:
                    return "Launcher.app";
                default:
                    throw new ArgumentOutOfRangeException("platformType", platformType, null);
            }
        }

        private static string FindLauncherExecutable(PlatformType platformType)
        {
            if (File.Exists(LauncherPathFileName))
            {
                var launcherPath = File.ReadAllText(LauncherPathFileName);
                if (File.Exists(launcherPath))
                {
                    return launcherPath;
                }
            }

            var defaultLauncherPath = Path.Combine("..", GetDefaultLauncherName(platformType));
            if (File.Exists(defaultLauncherPath))
            {
                return defaultLauncherPath;
            }

            throw new ApplicationException("Failed to find the Launcher executable.");
        }

        private static ProcessStartInfo GetLauncherProcessStartInfo(PlatformType platformType)
        {
            var launcherPath = Path.GetFullPath(FindLauncherExecutable(platformType));
            Logger.LogTrace("launcherPath = " + launcherPath);

            Logger.LogDebug("Checking if launcher is valid executable...");
            if (!Files.IsExecutable(launcherPath, platformType))
            {
                throw new ApplicationException("Invalid Launcher executable.");
            }

            switch (platformType)
            {
                case PlatformType.OSX:
                    return new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = string.Format("\"{0}\"", launcherPath)
                    };
                case PlatformType.Windows:
                case PlatformType.Linux:
                    return new ProcessStartInfo
                    {
                        FileName = launcherPath
                    };
                default:
                    throw new ArgumentOutOfRangeException("platformType", platformType, null);
            }
        }

        public static void ExecuteLauncher()
        {
            try
            {
                Logger.LogDebug("Executing launcher...");

                var platformType = Platform.GetPlatformType();
                Logger.LogTrace("platformType = " + platformType);

                var processStartInfo = GetLauncherProcessStartInfo(platformType);

                Logger.LogDebug("Starting launcher process...");
                Logger.LogTrace("fileName = " + processStartInfo.FileName);
                Logger.LogTrace("arguments = " + processStartInfo.Arguments);

                if (Process.Start(processStartInfo) == null)
                {
                    throw new ApplicationException("Failed to start Launcher process.");
                }

                Logger.LogDebug("Launcher executed.");
            }
            catch (Exception e)
            {
                Logger.LogError("Failed to execute launcher.", e);
                throw;
            }
        }
    }
}