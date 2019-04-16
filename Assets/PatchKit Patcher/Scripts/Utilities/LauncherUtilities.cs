using System;
using System.Diagnostics;
using System.IO;

namespace Utilities
{
    public static class LauncherUtilities
    {
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
            UnityEngine.Debug.Log("launcherPath = " + launcherPath);

            UnityEngine.Debug.Log("Checking if launcher is valid executable...");
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
                UnityEngine.Debug.Log("Executing launcher...");

                var platformType = Platform.GetPlatformType();
                UnityEngine.Debug.Log("platformType = " + platformType);

                var processStartInfo = GetLauncherProcessStartInfo(platformType);

                UnityEngine.Debug.Log("Starting launcher process...");
                UnityEngine.Debug.Log("fileName = " + processStartInfo.FileName);
                UnityEngine.Debug.Log("arguments = " + processStartInfo.Arguments);

                if (Process.Start(processStartInfo) == null)
                {
                    throw new ApplicationException("Failed to start Launcher process.");
                }

                UnityEngine.Debug.Log("Launcher executed.");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("Failed to execute launcher.");
                UnityEngine.Debug.LogException(e);
                throw;
            }
        }
    }
}