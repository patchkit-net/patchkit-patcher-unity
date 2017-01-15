using PatchKit.Api;
using PatchKit.Api.Models;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine;
using UnityEngine.Assertions;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class RemoteMetaData : IRemoteMetaData
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(RemoteMetaData));

        private readonly string _appSecret;
        private readonly MainApiConnection _mainApiConnection;
        private readonly KeysApiConnection _keysApiConnection;

        public RemoteMetaData(string appSecret, MainApiConnection mainApiConnection, KeysApiConnection keysApiConnection)
        {
            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(appSecret, "appSecret");

            Checks.ArgumentNotNullOrEmpty(appSecret, "appSecret");
            Assert.IsNotNull(mainApiConnection, "mainApiConnection");
            Assert.IsNotNull(keysApiConnection, "keysApiConnection");

            _appSecret = appSecret;
            _mainApiConnection = mainApiConnection;
            _keysApiConnection = keysApiConnection;
        }

        public int GetLatestVersionId()
        {
            DebugLogger.Log("Getting latest version id.");
            return _mainApiConnection.GetAppLatestAppVersionId(_appSecret).Id;
        }

        public App GetAppInfo()
        {
            DebugLogger.Log("Getting app info.");
            return _mainApiConnection.GetApplicationInfo(_appSecret);
        }

        public AppContentSummary GetContentSummary(int versionId)
        {
            DebugLogger.Log(string.Format("Getting content summary of version with id {0}.", versionId));
            Checks.ArgumentValidVersionId(versionId, "versionId");

            return _mainApiConnection.GetAppVersionContentSummary(_appSecret, versionId);
        }

        public AppDiffSummary GetDiffSummary(int versionId)
        {
            DebugLogger.Log(string.Format("Getting diff summary of version with id {0}.", versionId));
            Checks.ArgumentValidVersionId(versionId, "versionId");

            return _mainApiConnection.GetAppVersionDiffSummary(_appSecret, versionId);
        }

        public string GetKeySecret(string key)
        {
            DebugLogger.Log(string.Format("Getting key secret from key {0}.", key));
            Checks.ArgumentNotNullOrEmpty(key, "key");

            string cachedKeySecret = PlayerPrefs.GetString(GetCachedKeySecretEntryName(key), null);
            //TODO : Use cached key secret
            return _keysApiConnection.GetKeyInfo(key, _appSecret).KeySecret;
        }

        private string GetCachedKeySecretEntryName(string key)
        {
            return string.Format("patchkit-keysecret-{0}", key);
        }
    }
}