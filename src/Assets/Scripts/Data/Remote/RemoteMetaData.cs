using PatchKit.Api;
using PatchKit.Api.Models;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.Data.Remote
{
    internal class RemoteMetaData : IRemoteMetaData
    {
        private readonly DebugLogger _debugLogger;

        private readonly string _appSecret;
        private readonly MainApiConnection _mainApiConnection;
        private readonly KeysApiConnection _keysApiConnection;

        public RemoteMetaData(string appSecret, MainApiConnection mainApiConnection, KeysApiConnection keysApiConnection)
        {
            _debugLogger = new DebugLogger(this);

            _debugLogger.Log("Initialization");
            _debugLogger.LogTrace("appSecret = " + appSecret);

            _appSecret = appSecret;
            _mainApiConnection = mainApiConnection;
            _keysApiConnection = keysApiConnection;
        }

        public int GetLatestVersionId()
        {
            _debugLogger.Log("Getting latest version id.");
            return _mainApiConnection.GetAppLatestAppVersionId(_appSecret).Id;
        }

        public App GetAppInfo()
        {
            _debugLogger.Log("Getting app info.");
            return _mainApiConnection.GetApplicationInfo(_appSecret);
        }

        public AppContentSummary GetContentSummary(int versionId)
        {
            _debugLogger.Log("Getting content summary.");
            _debugLogger.LogTrace("versionId = " + versionId);
            return _mainApiConnection.GetAppVersionContentSummary(_appSecret, versionId);
        }

        public AppDiffSummary GetDiffSummary(int versionId)
        {
            _debugLogger.Log("Getting diff summary.");
            _debugLogger.LogTrace("versionId = " + versionId);
            return _mainApiConnection.GetAppVersionDiffSummary(_appSecret, versionId);
        }

        public string GetKeySecret(string key)
        {
            _debugLogger.Log("Getting key secret.");
            _debugLogger.LogTrace("key = " + key);
            return _keysApiConnection.GetKeyInfo(key, _appSecret).KeySecret;
        }
    }
}