using System;
using System.IO;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Utilities
{
    public class LauncherUtilities
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(LauncherUtilities));

        public const string LauncherPathFileName = "launcher_path";

        public static string LauncherExecutableByPlatform(PlatformType platform)
        {
            switch (platform)
            {
                case PlatformType.Linux:
                    return "Launcher";
                case PlatformType.Windows:
                    return "Launcher.exe";
                case PlatformType.OSX:
                    return "Launcher.app";
                default:
                    return "";
            }
        }

        public static string TryFindLauncherExecutable()
        {
            if (File.Exists(LauncherPathFileName))
            {
                var launcherPath = File.ReadAllText(LauncherPathFileName);
                return launcherPath;
            }

            var platformType = Platform.GetPlatformType();
            var executableName = Path.Combine("..", LauncherExecutableByPlatform(platformType));

            if (File.Exists(executableName))
            {
                return executableName;
            }

            throw new ApplicationException("Failed to find the Launcher executable.");
        }

        public static bool TryExecuteLauncher()
        {
            DebugLogger.Log("Trying to execute launcher.");

            var platformType = Platform.GetPlatformType();

            var executablePath = Path.GetFullPath(TryFindLauncherExecutable());

            if (!Files.IsExecutable(executablePath, platformType))
            {
                return false;
            }

            DebugLogger.Log(string.Format("Launcher executable has been resolved to {0}", executablePath));

            var process = ProcessUtils.Launch(executablePath);

            if (process == null)
            {
                return false;
            }

            return true;
        }
    }
}