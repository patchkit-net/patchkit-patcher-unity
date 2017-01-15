using System;
using System.Linq;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher
{
    public class App : IDisposable
    {
        public readonly ILocalData LocalData;

        public readonly IRemoteData RemoteData;

        public App(string appDataPath, string appSecret) : this(new LocalData(appDataPath), new RemoteData(appSecret))
        {
        }

        public App(ILocalData localData, IRemoteData remoteData)
        {
            AssertChecks.ArgumentNotNull(localData, "localData");
            AssertChecks.ArgumentNotNull(remoteData, "remoteData");

            LocalData = localData;
            RemoteData = remoteData;
        }

        public bool IsInstalled()
        {
            var fileNames = LocalData.MetaData.GetFileNames();

            if (fileNames.Length == 0)
            {
                return false;
            }

            int installedVersion = LocalData.MetaData.GetFileVersion(fileNames[0]);

            return fileNames.All(LocalData.FileExists) &&
                   fileNames.All(fileName => LocalData.MetaData.GetFileVersion(fileName) == installedVersion);
        }

        public int GetInstalledVersion()
        {
            AssertChecks.ApplicationIsInstalled(this);

            return LocalData.MetaData.GetFileVersion(LocalData.MetaData.GetFileNames()[0]);
        }

        public void Dispose()
        {
            LocalData.Dispose();
        }
    }
}