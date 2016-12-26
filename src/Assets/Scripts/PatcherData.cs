using PatchKit.Unity.Patcher.Data.Local;
using PatchKit.Unity.Patcher.Data.Remote;

namespace PatchKit.Unity.Patcher
{
    internal class PatcherData
    {
        public readonly ILocalData LocalData;

        public readonly IRemoteData RemoteData;

        public PatcherData(ILocalData localData, IRemoteData remoteData)
        {
            LocalData = localData;
            RemoteData = remoteData;
        }
    }
}