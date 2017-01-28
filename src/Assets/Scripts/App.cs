using System;
using System.IO;
using System.Linq;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher
{
    public class App : IDisposable
    {
        public readonly ILocalData LocalData;

        public readonly ILocalMetaData LocalMetaData;

        public readonly ITemporaryData TemporaryData;

        public readonly IDownloadData DownloadData;

        public readonly IRemoteData RemoteData;

        public readonly IRemoteMetaData RemoteMetaData;

        private int _overrideLatestVersionId;

        private bool _disposed;

        public App(string appDataPath, string appSecret, int overrideLatestVersionId) : this(
            CreateDefaultLocalData(appDataPath),
            CreateDefaultLocalMetaData(appDataPath),
            CreateDefaultTemporaryData(appDataPath),
            CreateDefaultDownloadData(appDataPath),
            CreateDefaultRemoteData(appSecret),
            CreateDefaultRemoteMetaData(appSecret), overrideLatestVersionId)
        {
        }

        public App(ILocalData localData, ILocalMetaData localMetaData, ITemporaryData temporaryData, IDownloadData downloadData, IRemoteData remoteData, IRemoteMetaData remoteMetaData, int overrideLatestVersionId)
        {
            AssertChecks.ArgumentNotNull(localData, "localData");
            AssertChecks.ArgumentNotNull(localMetaData, "localMetaData");
            AssertChecks.ArgumentNotNull(temporaryData, "temporaryData");
            AssertChecks.ArgumentNotNull(downloadData, "downloadData");
            AssertChecks.ArgumentNotNull(remoteData, "remoteData");
            AssertChecks.ArgumentNotNull(remoteMetaData, "remoteMetaData");

            LocalData = localData;
            LocalMetaData = localMetaData;
            TemporaryData = temporaryData;
            DownloadData = downloadData;
            RemoteData = remoteData;
            RemoteMetaData = remoteMetaData;
            _overrideLatestVersionId = overrideLatestVersionId;
        }

        public bool IsInstalled()
        {
            var fileNames = LocalMetaData.GetFileNames();

            if (fileNames.Length == 0)
            {
                return false;
            }

            int installedVersion = LocalMetaData.GetFileVersionId(fileNames[0]);

            return fileNames.All(LocalData.FileExists) &&
                   fileNames.All(fileName => LocalMetaData.GetFileVersionId(fileName) == installedVersion);
        }

        public int GetInstalledVersionId()
        {
            AssertChecks.ApplicationIsInstalled(this);

            return LocalMetaData.GetFileVersionId(LocalMetaData.GetFileNames()[0]);
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
                LocalData.Dispose();
            }

            _disposed = true;
        }

        private static ILocalData CreateDefaultLocalData(string appDataPath)
        {
            return new LocalData(appDataPath);
        }

        private static ITemporaryData CreateDefaultTemporaryData(string appDataPath)
        {
            return new TemporaryData(Path.Combine(appDataPath, ".temp"));
        }

        private static IDownloadData CreateDefaultDownloadData(string appDataPath)
        {
            return new DownloadData(Path.Combine(appDataPath, ".downloads"));
        }

        private static ILocalMetaData CreateDefaultLocalMetaData(string appDataPath)
        {
            return new LocalMetaData(Path.Combine(appDataPath, "patcher_cache.json"));
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