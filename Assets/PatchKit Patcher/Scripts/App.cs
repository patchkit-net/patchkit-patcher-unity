using System;
using System.IO;
using JetBrains.Annotations;
using PatchKit.Network;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher
{
    public class App
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(App));

        public readonly ILocalDirectory LocalDirectory;

        public readonly ILocalMetaData LocalMetaData;

        public readonly IDownloadDirectory DownloadDirectory;

        public readonly IRemoteData RemoteData;

        public readonly IRemoteMetaData RemoteMetaData;

        private readonly string _appDataPath;

        private readonly int _overrideLatestVersionId;

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

            _appDataPath = appDataPath;
            LocalDirectory = localDirectory;
            LocalMetaData = localMetaData;
            DownloadDirectory = downloadDirectory;
            RemoteData = remoteData;
            RemoteMetaData = remoteMetaData;
            _overrideLatestVersionId = overrideLatestVersionId;
        }

        public bool IsInstalled()
        {
            var fileNames = LocalMetaData.GetRegisteredEntries();

            if (fileNames.Length == 0)
            {
                return false;
            }

            int installedVersion = LocalMetaData.GetEntryVersionId(fileNames[0]);

            foreach (string fileName in fileNames)
            {
                string path = LocalDirectory.Path.PathCombine(fileName);
                if (!File.Exists(path))
                {
                    DebugLogger.LogWarning("File in metadata, but not found on disk: " + fileName + ", search path: " +
                                           path);
                    return false;
                }

                int fileVersion = LocalMetaData.GetEntryVersionId(fileName);
                if (fileVersion != installedVersion)
                {
                    DebugLogger.LogWarning("File " + fileName + " installed version is " + fileVersion +
                                           " but expected " + installedVersion);
                    return false;
                }
            }

            return true;
        }

        public int GetInstalledVersionId()
        {
            Assert.ApplicationIsInstalled(this);

            return LocalMetaData.GetEntryVersionId(LocalMetaData.GetRegisteredEntries()[0]);
        }

        public int GetLatestVersionId()
        {
            if (_overrideLatestVersionId > 0)
            {
                return _overrideLatestVersionId;
            }

            return RemoteMetaData.GetLatestVersionId();
        }

        private static ILocalDirectory CreateDefaultLocalDirectory(string appDataPath)
        {
            return new LocalDirectory(appDataPath);
        }

        private static IDownloadDirectory CreateDefaultDownloadDirectory(string appDataPath)
        {
            return new DownloadDirectory(appDataPath.PathCombine(".downloads"));
        }

        private static ILocalMetaData CreateDefaultLocalMetaData(string appDataPath)
        {
            return new LocalMetaData(appDataPath.PathCombine("app_data.json"),
                appDataPath.PathCombine("patcher_cache.json"));
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