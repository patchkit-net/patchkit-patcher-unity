using PatchKit.Unity.Patcher.Data.Local;
using PatchKit.Unity.Patcher.Data.Remote;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher
{
    internal class PatcherData
    {
        public readonly ILocalData LocalData;

        public readonly IRemoteData RemoteData;

        public PatcherData(ILocalData localData, IRemoteData remoteData)
        {
            AssertChecks.ArgumentNotNull(localData, "localData");
            AssertChecks.ArgumentNotNull(remoteData, "remoteData");

            LocalData = localData;
            RemoteData = remoteData;
        }
    }
}