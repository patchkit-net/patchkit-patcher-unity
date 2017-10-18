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
            _mainApiConnection = new MainApiConnection(Settings.GetMainApiConnectionSettings());

            var keysSettings = Settings.GetKeysApiConnectionSettings();

            string overrideKeysUrl;

            if (TryReadEnvironmentVariable("PK_PATCHER_KEYS_URL", out overrideKeysUrl))
            {
                bool useHttps = overrideKeysUrl.StartsWith("https://");

                overrideKeysUrl = overrideKeysUrl.Replace("https://", string.Empty)
                    .Replace("http://", string.Empty);

                keysSettings.MainServer.Host = overrideKeysUrl;
                keysSettings.MainServer.Port = 0;
                keysSettings.MainServer.UseHttps = useHttps;
            }

            _keysApiConnection =
                new KeysApiConnection(keysSettings)
                {
                    HttpWebRequestFactory = new UnityWebRequestFactory()
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

        private static bool TryReadEnvironmentVariable(string argumentName, out string value)
        {
            value = Environment.GetEnvironmentVariable(argumentName);

            return value != null;
        }
    }
}