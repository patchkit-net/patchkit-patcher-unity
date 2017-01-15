using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData
{
    public class AppData
    {
        public readonly ILocalData LocalData;

        public readonly IRemoteData RemoteData;

        public AppData(ILocalData localData, IRemoteData remoteData)
        {
            AssertChecks.ArgumentNotNull(localData, "localData");
            AssertChecks.ArgumentNotNull(remoteData, "remoteData");

            LocalData = localData;
            RemoteData = remoteData;
        }
    }
}