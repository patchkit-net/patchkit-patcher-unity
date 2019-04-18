using System;
using System.IO;
using JetBrains.Annotations;
using PatchKit.Network;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher
{
    public class App
    {
        private const string AppCacheFlieName = "patcher_cache.json";

        private const string AppDataFileName = "app_data.json";

        private const string DownloadsDirName = ".downloads";

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(App));

        public readonly ILocalDirectory LocalDirectory;

        public readonly ILocalMetaData LocalMetaData;

        public readonly IDownloadDirectory DownloadDirectory;

        public readonly IRemoteData RemoteData;

        public readonly IRemoteMetaData RemoteMetaData;

        private readonly int _overrideLatestVersionId;

        public enum InstallStatus
        {
            NotInstalled = 0,
            Broken = 1,
            Installed = 2
        }

        public App(string appDataPath, string appSecret, int overrideLatestVersionId,
            IRequestTimeoutCalculator requestTimeoutCalculator) : this(
            appDataPath,
            CreateDefaultLocalDirectory(appDataPath),
            CreateDefaultLocalMetaData(appDataPath),
            CreateDefaultDownloadDirectory(appDataPath),
            CreateDefaultRemoteData(appSecret, requestTimeoutCalculator),
            CreateDefaultRemoteMetaData(appSecret, requestTimeoutCalculator), overrideLatestVersionId)
        {
        }

        public App([NotNull] string appDataPath, [NotNull] ILocalDirectory localDirectory,
            [NotNull] ILocalMetaData localMetaData,
            [NotNull] IDownloadDirectory downloadDirectory, [NotNull] IRemoteData remoteData,
            [NotNull] IRemoteMetaData remoteMetaData,
            int overrideLatestVersionId)
        {
            if (string.IsNullOrEmpty(appDataPath))
            {
                throw new ArgumentException("Value cannot be null or empty.", "appDataPath");
            }

            if (localDirectory == null)
            {
                throw new ArgumentNullException("localDirectory");
            }

            if (localMetaData == null)
            {
                throw new ArgumentNullException("localMetaData");
            }

            if (downloadDirectory == null)
            {
                throw new ArgumentNullException("downloadDirectory");
            }

            if (remoteData == null)
            {
                throw new ArgumentNullException("remoteData");
            }

            if (remoteMetaData == null)
            {
                throw new ArgumentNullException("remoteMetaData");
            }

            LocalDirectory = localDirectory;
            LocalMetaData = localMetaData;
            DownloadDirectory = downloadDirectory;
            RemoteData = remoteData;
            RemoteMetaData = remoteMetaData;
            _overrideLatestVersionId = overrideLatestVersionId;
        }

        public InstallStatus GetInstallStatus()
        {
            var fileNames = LocalMetaData.GetRegisteredEntries();

            if (fileNames.Length == 0)
            {
                return InstallStatus.NotInstalled;
            }

            int installedVersion = LocalMetaData.GetEntryVersionId(fileNames[0]);

            foreach (string fileName in fileNames)
            {
                string path = LocalDirectory.Path.PathCombine(fileName);
                if (!File.Exists(path))
                {
                    DebugLogger.LogWarning("File in metadata, but not found on disk: " + fileName + ", search path: " +
                                           path);
                    return InstallStatus.Broken;
                }

                int fileVersion = LocalMetaData.GetEntryVersionId(fileName);
                if (fileVersion != installedVersion)
                {
                    DebugLogger.LogWarning("File " + fileName + " installed version is " + fileVersion +
                                           " but expected " + installedVersion);
                    return InstallStatus.Broken;
                }
            }

            return InstallStatus.Installed;
        }

        public bool IsFullyInstalled()
        {
            return GetInstallStatus() == InstallStatus.Installed;
        }

        public bool IsInstallationBroken()
        {
            return GetInstallStatus() == InstallStatus.Broken;
        }

        public bool IsNotInstalled()
        {
            return GetInstallStatus() == InstallStatus.NotInstalled;
        }

        public int GetInstalledVersionId()
        {
            Assert.ApplicationIsInstalled(this);

            return LocalMetaData.GetEntryVersionId(LocalMetaData.GetRegisteredEntries()[0]);
        }

        public int GetLatestVersionId(bool retryRequests, CancellationToken cancellationToken)
        {
            if (_overrideLatestVersionId > 0)
            {
                return _overrideLatestVersionId;
            }

            return RemoteMetaData.GetLatestVersionId(retryRequests, cancellationToken);
        }

        public int GetLowestVersionWithDiffId(CancellationToken cancellationToken)
        {
            var appInfo = RemoteMetaData.GetAppInfo(true, cancellationToken);
            return appInfo.LowestVersionWithDiff;
        }

        public int GetLowestVersionWithContentId(CancellationToken cancellationToken)
        {
            var appInfo = RemoteMetaData.GetAppInfo(true, cancellationToken);
            return appInfo.LowestVersionWithContent;
        }

        private static ILocalDirectory CreateDefaultLocalDirectory(string appDataPath)
        {
            return new LocalDirectory(appDataPath);
        }

        private static IDownloadDirectory CreateDefaultDownloadDirectory(string appDataPath)
        {
            return new DownloadDirectory(appDataPath.PathCombine(DownloadsDirName));
        }

        private static ILocalMetaData CreateDefaultLocalMetaData(string appDataPath)
        {
            return new LocalMetaData(appDataPath.PathCombine(AppDataFileName),
                appDataPath.PathCombine(AppCacheFlieName));
        }

        private static IRemoteData CreateDefaultRemoteData(string appSecret,
            IRequestTimeoutCalculator requestTimeoutCalculator)
        {
            return new RemoteData(appSecret, requestTimeoutCalculator);
        }

        private static IRemoteMetaData CreateDefaultRemoteMetaData(string appSecret,
            IRequestTimeoutCalculator requestTimeoutCalculator)
        {
            return new RemoteMetaData(appSecret, requestTimeoutCalculator);
        }
    }
}