using System;
using System.Diagnostics;
using System.IO;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.Cancellation;
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

        private string ResolveExecutablePath(AppVersion? appVersion)
        {
            PlatformType platformType = Platform.GetPlatformType();

            if (appVersion.HasValue && 
                !string.IsNullOrEmpty(appVersion.Value.MainExecutable))
            {
                string executablePath = Path.Combine(
                    _app.LocalDirectory.Path, 
                    appVersion.Value.MainExecutable);

                bool isOSXApp = platformType == PlatformType.OSX &&
                                executablePath.EndsWith(".app") &&
                                Directory.Exists(executablePath);
                
                if (File.Exists(executablePath) || isOSXApp)
                {
                    return executablePath;
                }

                // Reports to Sentry
                try
                {
                    throw new FileNotFoundException(string.Format("Couldn't resolve executable in {0}", executablePath));
                }
                catch (Exception e)
                {
                    DebugLogger.LogException(e);
                }

            }

            return AppFinder.FindExecutable(_app.LocalDirectory.Path, platformType);
        }

        public void Start(string customArgs)
        {
            AppVersion? appVersion = null;

            try
            {
                appVersion = _app.RemoteMetaData.GetAppVersionInfo(
                    _app.GetInstalledVersionId(),
                    false,
                    CancellationToken.Empty);
            }
            catch (Exception e)
            {
                DebugLogger.LogException(e);
                DebugLogger.LogWarning(
                    "Failed to retrieve app version info. Will try to detect exectuable manually.");
            }
        
            StartAppVersion(appVersion, customArgs);
        }

        private void StartAppVersion(AppVersion? appVersion, string customArgs)
        {
            DebugLogger.Log("Starting application.");

            PlatformType platformType = Platform.GetPlatformType();
            string appFilePath = ResolveExecutablePath(appVersion);
            string appArgs = customArgs ?? string.Empty;

            if (appVersion != null &&
                appVersion.Value.MainExecutableArgs != null)
            {
                appArgs += " " + appVersion.Value.MainExecutableArgs;
            }

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

            var processStartInfo = GetProcessStartInfo(appFilePath, appArgs, platformType);

            StartAppProcess(processStartInfo);
        }

        private bool NeedPermissionFix(PlatformType platformType)
        {
            return platformType == PlatformType.OSX || platformType == PlatformType.Linux;
        }

        private ProcessStartInfo GetProcessStartInfo(string executablePath, string mainExecutableArgs, PlatformType platform)
        {
            if (mainExecutableArgs == null)
            {
                mainExecutableArgs = string.Empty;
            }

            string workingDir = Path.GetDirectoryName(executablePath) ?? string.Empty;
            switch (platform)
            {
                case PlatformType.Unknown:
                    throw new ArgumentException("Unknown");
                case PlatformType.Windows:
                    return new ProcessStartInfo
                    {
                        FileName = executablePath,
                        Arguments = string.Format("+patcher-data-location \"{0}\" " + mainExecutableArgs, _app.LocalMetaData.GetFilePath()),
                        WorkingDirectory = workingDir
                    };
                case PlatformType.OSX:
                    if (!string.IsNullOrEmpty(mainExecutableArgs))
                    {
                        mainExecutableArgs = " --args " + mainExecutableArgs;
                    }

                    return new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = string.Format("\"{0}\"{1}", executablePath, mainExecutableArgs),
                        WorkingDirectory = workingDir
                    };
                case PlatformType.Linux:
                    return new ProcessStartInfo
                    {
                        FileName = executablePath,
                        Arguments = mainExecutableArgs,
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