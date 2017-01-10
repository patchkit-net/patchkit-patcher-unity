using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;

namespace PatchKit.Unity.Patcher
{
    internal class ApplicationStarter
    {
        private readonly ILocalData _localData;
        private readonly IRemoteData _remoteData;

        public ApplicationStarter(ILocalData localData, IRemoteData remoteData)
        {
            _localData = localData;
            _remoteData = remoteData;
        }

        public void Start()
        {
            
        }
    }
}
