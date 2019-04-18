using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace Deprecated
{
public static class LauncherUtilities
{
    private const string LauncherPathFileName = "launcher_path";

    [NotNull]
    private static string GetDefaultLauncherName()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.LinuxEditor:
            case RuntimePlatform.LinuxPlayer:
                return "Launcher";
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
                return "Launcher.exe";
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.OSXPlayer:
                return "Launcher.app";
            default:
                throw new NotSupportedException();
        }
    }

    private static string FindLauncherExecutable()
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

        return File.Exists(path: defaultLauncherPath)
            ? defaultLauncherPath
            : null;
    }

    private static ProcessStartInfo GetLauncherProcessStartInfo()
    {
        string launcherPath = Path.GetFullPath(path: FindLauncherExecutable());

        if (!Files.IsExecutable(
            filePath: launcherPath))
        {
            throw new ApplicationException(
                message: "Invalid Launcher executable.");
        }

        switch (Application.platform)
        {
            case RuntimePlatform.LinuxEditor:
            case RuntimePlatform.LinuxPlayer:
                return new ProcessStartInfo
                {
                    FileName = launcherPath
                };
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
                return new ProcessStartInfo
                {
                    FileName = launcherPath
                };
            case RuntimePlatform.OSXEditor:
            case RuntimePlatform.OSXPlayer:
                return new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = string.Format(
                        format: "\"{0}\"",
                        arg0: launcherPath)
                };
            default:
                throw new NotSupportedException();
        }
    }

    public static void ExecuteLauncher()
    {
        var processStartInfo =
            GetLauncherProcessStartInfo();

        if (Process.Start(startInfo: processStartInfo) == null)
        {
            throw new ApplicationException(
                message: "Failed to start Launcher process.");
        }
    }
}
}