using System;
using System.Diagnostics;
using System.IO;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.AppData;

namespace PatchKit.Unity.Utilities
{
    public static class LauncherUtilities
    {
        private static readonly ILogger Logger = PatcherLogManager.DefaultLogger;

        private const string LauncherPathFileName = "launcher_path";
        private static readonly string[] LauncherPathFileSearchLocations = {".", "..", "../..", "patcher", "Patcher"};
        private static readonly string[] LauncherExeSearchLocations = {".", "..", "../.."};

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

        public static string FindLauncherExecutable(PlatformType platformType)
        {
            var launcherPathFile = FileOperations.SearchPathsFindOne(LauncherPathFileName, LauncherPathFileSearchLocations);

            if (File.Exists(launcherPathFile))
            {
                var relativeLauncherPath = File.ReadAllText(launcherPathFile);
                var effectiveLauncherPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(launcherPathFile), relativeLauncherPath));
                if (File.Exists(effectiveLauncherPath))
                {
                    Logger.LogTrace("Launcher path resolved from file to " + effectiveLauncherPath);
                    return effectiveLauncherPath;
                }
            }
            var defaultLauncherFilename = GetDefaultLauncherName(platformType);
            var defaultLauncherPath = FileOperations.SearchPathsFindOne(defaultLauncherFilename, LauncherExeSearchLocations);

            if (File.Exists(defaultLauncherPath))
            {
                Logger.LogTrace("Found launcher in " + defaultLauncherPath);
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