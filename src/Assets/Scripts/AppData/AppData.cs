using PatchKit.Api;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData
{
    internal class AppData
    {
        public readonly ILocalData LocalData;

        public readonly IRemoteData RemoteData;

        public AppData(string appSecret, string appDataPath) : this(
            CreateDefaultLocalData(appDataPath),
            CreateDefaultRemoteData(appSecret))
        {
        }

        public AppData(ILocalData localData, IRemoteData remoteData)
        {
            AssertChecks.ArgumentNotNull(localData, "localData");
            AssertChecks.ArgumentNotNull(remoteData, "remoteData");

            LocalData = localData;
            RemoteData = remoteData;
        }

        private static ILocalData CreateDefaultLocalData(string appDataPath)
        {
            return new LocalData(appDataPath);
        }

        private static IRemoteData CreateDefaultRemoteData(string appSecret)
        {
            var mainApiConnection = new MainApiConnection(Settings.GetMainApiConnectionSettings());
            var keysApiConnection = new KeysApiConnection(Settings.GetMainApiConnectionSettings());

            return new RemoteData(appSecret, mainApiConnection, keysApiConnection);
        }
    }
}