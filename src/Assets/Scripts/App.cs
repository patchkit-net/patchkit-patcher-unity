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

        public App(string appDataPath, string appSecret) : this(
            CreateDefaultLocalData(appDataPath),
            CreateDefaultLocalMetaData(appDataPath),
            CreateDefaultTemporaryData(appDataPath),
            CreateDefaultDownloadData(appDataPath),
            CreateDefaultRemoteData(appSecret),
            CreateDefaultRemoteMetaData(appSecret))
        {
        }

        public App(ILocalData localData, ILocalMetaData localMetaData, ITemporaryData temporaryData, IDownloadData downloadData, IRemoteData remoteData, IRemoteMetaData remoteMetaData)
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

        public void Dispose()
        {
            LocalData.Dispose();
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