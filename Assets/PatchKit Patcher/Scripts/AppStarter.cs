using System;
using System.Diagnostics;
using System.IO;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.Data;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher
{
    public class AppStarter
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppStarter));

        private readonly App _app;

        public AppFinder AppFinder { get; set; }

        public AppStarter(App app)
        {
            Checks.ArgumentNotNull(app, "app");

            DebugLogger.LogConstructor();

            _app = app;
            AppFinder = new AppFinder();
        }

        private string ResolveExecutablePath(AppVersion appVersion)
        {
            if (!string.IsNullOrEmpty(appVersion.MainExecutable))
            {
                string executablePath = Path.Combine(_app.LocalDirectory.Path, appVersion.MainExecutable);

                if (File.Exists(executablePath))
                {
                    return executablePath;
                }

                // Reports to Sentry
                DebugLogger.LogException(
                    new FileNotFoundException(string.Format("Couldn't resolve executable in {0}", executablePath)));
            }

            PlatformType platformType = Platform.GetPlatformType();
            return AppFinder.FindExecutable(_app.LocalDirectory.Path, platformType);
        }

        public void Start()
        {
            var appVersion = _app.RemoteMetaData.GetAppVersionInfo(_app.GetInstalledVersionId());
            StartAppVersion(appVersion);
        }

        private void StartAppVersion(AppVersion appVersion)
        {
            DebugLogger.Log("Starting application.");

            PlatformType platformType = Platform.GetPlatformType();
            string appFilePath = ResolveExecutablePath(appVersion);

            if (appFilePath == null)
            {
                throw new InvalidOperationException("Couldn't find executable.");
            }

            DebugLogger.Log(string.Format("Found executable {0}", appFilePath));

            if (NeedPermissionFix(platformType))
            {
                foreach (var fileName in _app.LocalMetaData.GetRegisteredEntries())
                {
                    string filePath = _app.LocalDirectory.Path.PathCombine(fileName);
                    if (Files.IsExecutable(filePath, platformType))
                    {
                        DebugLogger.LogFormat("File is recognized as executable {0}", filePath);
                        Chmod.SetExecutableFlag(filePath);
                    }
                }
            }

            var processStartInfo = GetProcessStartInfo(appFilePath, platformType);

            if (!string.IsNullOrEmpty(appVersion.MainExecutableArgs))
            {
                processStartInfo.Arguments += " " + appVersion.MainExecutableArgs;
            }

            StartAppProcess(processStartInfo);
        }

        private bool NeedPermissionFix(PlatformType platformType)
        {
            return platformType == PlatformType.OSX || platformType == PlatformType.Linux;
        }

        private ProcessStartInfo GetProcessStartInfo(string executablePath, PlatformType platform)
        {
            string workingDir = Path.GetDirectoryName(executablePath) ?? string.Empty;
            switch (platform)
            {
                case PlatformType.Unknown:
                    throw new ArgumentException("Unknown");
                case PlatformType.Windows:
                    return new ProcessStartInfo
                    {
                        FileName = executablePath,
                        Arguments = string.Format("+patcher-data-location \"{0}\"", _app.LocalMetaData.GetFilePath()),
                        WorkingDirectory = workingDir
                    };
                case PlatformType.OSX:
                    return new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = string.Format("\"{0}\"", executablePath),
                        WorkingDirectory = workingDir
                    };
                case PlatformType.Linux:
                    return new ProcessStartInfo
                    {
                        FileName = executablePath,
                        WorkingDirectory = workingDir
                    };
                default:
                    throw new ArgumentOutOfRangeException("platform", platform, null);
            }
        }

        private void StartAppProcess(ProcessStartInfo processStartInfo)
        {
            DebugLogger.Log(string.Format("Starting process '{0}' with arguments '{1}'", processStartInfo.FileName,
                processStartInfo.Arguments));

            var process = Process.Start(processStartInfo);
            if (process == null)
            {
                DebugLogger.LogError(string.Format("Failed to start process {0}", processStartInfo.FileName));
            }
            else if (process.HasExited)
            {
                DebugLogger.LogError(string.Format("Process '{0}' prematurely exited with code '{1}'",
                    processStartInfo.FileName, process.ExitCode));
            }
        }
    }
}