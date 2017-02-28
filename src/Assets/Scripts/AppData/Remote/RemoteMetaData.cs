using PatchKit.Api;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class RemoteMetaData : IRemoteMetaData
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(RemoteMetaData));

        private readonly string _appSecret;
        private readonly MainApiConnection _mainApiConnection;
        private readonly KeysApiConnection _keysApiConnection;

        public RemoteMetaData(string appSecret) : this(appSecret, 
            new MainApiConnection(Settings.GetMainApiConnectionSettings()),
            new KeysApiConnection(Settings.GetKeysApiConnectionSettings()))
        {
        }

        public RemoteMetaData(string appSecret, MainApiConnection mainApiConnection, KeysApiConnection keysApiConnection)
        {
            Checks.ArgumentNotNullOrEmpty(appSecret, "appSecret");
            Checks.ArgumentNotNull(mainApiConnection, "mainApiConnection");
            Checks.ArgumentNotNull(keysApiConnection, "keysApiConnection");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(appSecret, "appSecret");

            _appSecret = appSecret;
            _mainApiConnection = mainApiConnection;
            _keysApiConnection = keysApiConnection;
        }

        public int GetLatestVersionId()
        {
            DebugLogger.Log("Getting latest version id.");
            return _mainApiConnection.GetAppLatestAppVersionId(_appSecret).Id;
        }

        public Api.Models.Main.App GetAppInfo()
        {
            DebugLogger.Log("Getting app info.");
            return _mainApiConnection.GetApplicationInfo(_appSecret);
        }

        public AppContentSummary GetContentSummary(int versionId)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            DebugLogger.Log(string.Format("Getting content summary of version with id {0}.", versionId));

            return _mainApiConnection.GetAppVersionContentSummary(_appSecret, versionId);
        }

        public AppDiffSummary GetDiffSummary(int versionId)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            DebugLogger.Log(string.Format("Getting diff summary of version with id {0}.", versionId));

            return _mainApiConnection.GetAppVersionDiffSummary(_appSecret, versionId);
        }

        public string GetKeySecret(string key)
        {
            Checks.ArgumentNotNullOrEmpty(key, "key");
            DebugLogger.Log(string.Format("Getting key secret from key {0}.", key));

            string cachedKeySecret = null;

            Dispatcher.Invoke(() => cachedKeySecret = PlayerPrefs.GetString(GetCachedKeySecretEntryName(key), null));

            var keySecret = _keysApiConnection.GetKeyInfo(key, _appSecret, cachedKeySecret).KeySecret;

            Dispatcher.Invoke(() => PlayerPrefs.SetString(GetCachedKeySecretEntryName(key), keySecret));

            return keySecret;
        }

        private string GetCachedKeySecretEntryName(string key)
        {
            return string.Format("patchkit-keysecret-{0}", key);
        }
    }
}