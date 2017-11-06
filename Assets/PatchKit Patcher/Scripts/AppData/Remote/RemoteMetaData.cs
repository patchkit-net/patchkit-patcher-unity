using System;
using PatchKit.Api;
using PatchKit.Api.Models.Main;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class RemoteMetaData : IRemoteMetaData
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(RemoteMetaData));

        private readonly string _appSecret;
        private readonly MainApiConnection _mainApiConnection;
        private readonly KeysApiConnection _keysApiConnection;

        public RemoteMetaData(string appSecret)
        {
            Checks.ArgumentNotNullOrEmpty(appSecret, "appSecret");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(appSecret, "appSecret");

            _appSecret = appSecret;

            var mainSettings = Settings.GetMainApiConnectionSettings();

            string overrideMainUrl;

            if (EnvironmentInfo.TryReadEnvironmentVariable(EnvironmentVariables.MainUrlEnvironmentVariable, out overrideMainUrl))
            {
                var overrideMainUri = new Uri(overrideMainUrl);

                mainSettings.MainServer.Host = overrideMainUri.Host;
                mainSettings.MainServer.Port = overrideMainUri.Port;
                mainSettings.MainServer.UseHttps = overrideMainUri.Scheme == Uri.UriSchemeHttps;
            }

            _mainApiConnection = new MainApiConnection(mainSettings)
            {
                HttpWebRequestFactory = new UnityWebRequestFactory(),
                Logger = PatcherLogManager.DefaultLogger
            };

            var keysSettings = Settings.GetKeysApiConnectionSettings();

            string overrideKeysUrl;

            if (EnvironmentInfo.TryReadEnvironmentVariable(EnvironmentVariables.KeysUrlEnvironmentVariable, out overrideKeysUrl))
            {
                var overrideKeysUri = new Uri(overrideKeysUrl);

                keysSettings.MainServer.Host = overrideKeysUri.Host;
                keysSettings.MainServer.Port = overrideKeysUri.Port;
                keysSettings.MainServer.UseHttps = overrideKeysUri.Scheme == Uri.UriSchemeHttps;
            }

            _keysApiConnection = new KeysApiConnection(keysSettings)
            {
                HttpWebRequestFactory = new UnityWebRequestFactory(),
                Logger = PatcherLogManager.DefaultLogger
            };

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

        public string GetKeySecret(string key, string cachedKeySecret)
        {
            Checks.ArgumentNotNullOrEmpty(key, "key");
            DebugLogger.Log(string.Format("Getting key secret from key {0}.", key));

            var keySecret = _keysApiConnection.GetKeyInfo(key, _appSecret, cachedKeySecret).KeySecret;

            return keySecret;
        }
    }
}