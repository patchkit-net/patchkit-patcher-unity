using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
    private const string LauncherPathFileName = "launcher_path";

    [NotNull]
    private string GetDefaultLauncherName()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.LinuxPlayer:
                return "Launcher";
            case RuntimePlatform.WindowsPlayer:
                return "Launcher.exe";
            case RuntimePlatform.OSXPlayer:
                return "Launcher.app";
            default:
                throw new NotSupportedException();
        }
    }

    private string GetLauncherPath()
    {
        if (File.Exists(path: LauncherPathFileName))
        {
            string launcherPath = File.ReadAllText(path: LauncherPathFileName);
            if (File.Exists(path: launcherPath))
            {
                return launcherPath;
            }
        }

        string defaultLauncherPath = Path.Combine(
            path1: "..",
            path2: GetDefaultLauncherName());

        defaultLauncherPath = Path.GetFullPath(path: defaultLauncherPath);

        return File.Exists(path: defaultLauncherPath)
            ? defaultLauncherPath
            : null;
    }

    private ProcessStartInfo GetLauncherProcessStartInfo()
    {
        string launcherPath = GetLauncherPath();

        //TODO: Expose in libpkapps function to check if file is executable
        /*if (!Files.IsExecutable(
            filePath: launcherPath))
        {
            return null;
        }*/

        switch (Application.platform)
        {
            case RuntimePlatform.LinuxPlayer:
                return new ProcessStartInfo
                {
                    FileName = launcherPath
                };
            case RuntimePlatform.WindowsPlayer:
                return new ProcessStartInfo
                {
                    FileName = launcherPath
                };
            case RuntimePlatform.OSXPlayer:
                return new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"\"{launcherPath}\""
                };
            default:
                throw new NotSupportedException();
        }
    }

    private async Task<bool> TryToRestartWithLauncher()
    {
        var processStartInfo =
            GetLauncherProcessStartInfo();

        if (processStartInfo == null)
        {
            return false;
        }

        if (Process.Start(startInfo: processStartInfo) == null)
        {
            return false;
        }

        return true;
    }
}