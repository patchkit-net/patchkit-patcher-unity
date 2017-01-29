using System;
using System.IO;
using System.Linq;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher
{
    public class App : IDisposable
    {
        public readonly ILocalDirectory LocalDirectory;

        public readonly ILocalMetaData LocalMetaData;

        public readonly ITemporaryDirectory TemporaryDirectory;

        public readonly IDownloadDirectory DownloadDirectory;

        public readonly IRemoteData RemoteData;

        public readonly IRemoteMetaData RemoteMetaData;

        private readonly int _overrideLatestVersionId;

        private bool _disposed;

        public App(string appDataPath, string appSecret, int overrideLatestVersionId) : this(
            CreateDefaultLocalDirectory(appDataPath),
            CreateDefaultLocalMetaData(appDataPath),
            CreateDefaultTemporaryDirectory(appDataPath),
            CreateDefaultDownloadDirectory(appDataPath),
            CreateDefaultRemoteData(appSecret),
            CreateDefaultRemoteMetaData(appSecret), overrideLatestVersionId)
        {
        }

        public App(ILocalDirectory localDirectory, ILocalMetaData localMetaData, ITemporaryDirectory temporaryDirectory, IDownloadDirectory downloadDirectory, IRemoteData remoteData, IRemoteMetaData remoteMetaData, int overrideLatestVersionId)
        {
            AssertChecks.ArgumentNotNull(localDirectory, "localData");
            AssertChecks.ArgumentNotNull(localMetaData, "localMetaData");
            AssertChecks.ArgumentNotNull(temporaryDirectory, "temporaryData");
            AssertChecks.ArgumentNotNull(downloadDirectory, "downloadData");
            AssertChecks.ArgumentNotNull(remoteData, "remoteData");
            AssertChecks.ArgumentNotNull(remoteMetaData, "remoteMetaData");

            LocalDirectory = localDirectory;
            LocalMetaData = localMetaData;
            TemporaryDirectory = temporaryDirectory;
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

            return fileNames.All(fileName => File.Exists(LocalDirectory.Path.PathCombine(fileName)) &&
                                             LocalMetaData.GetEntryVersionId(fileName) == installedVersion);
        }

        public int GetInstalledVersionId()
        {
            AssertChecks.ApplicationIsInstalled(this);

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

        public void Dispose()
        {
            Dispose(false);
        }

        ~App()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                TemporaryDirectory.Dispose();
            }

            _disposed = true;
        }

        private static ILocalDirectory CreateDefaultLocalDirectory(string appDataPath)
        {
            return new LocalDirectory(appDataPath);
        }

        private static ITemporaryDirectory CreateDefaultTemporaryDirectory(string appDataPath)
        {
            return new TemporaryDirectory(appDataPath.PathCombine(".temp"));
        }

        private static IDownloadDirectory CreateDefaultDownloadDirectory(string appDataPath)
        {
            return new DownloadDirectory(appDataPath.PathCombine(".downloads"));
        }

        private static ILocalMetaData CreateDefaultLocalMetaData(string appDataPath)
        {
            return new LocalMetaData(appDataPath.PathCombine("patcher_cache.json"));
        }

        private static IRemoteData CreateDefaultRemoteData(string appSecret)
        {
            return new RemoteData(appSecret);
        }

        private static IRemoteMetaData CreateDefaultRemoteMetaData(string appSecret)
        {
            return new RemoteMetaData(appSecret);
        }
    }
}