﻿using System;
using JetBrains.Annotations;
using PatchKit.Api;
using PatchKit.Api.Models.Main;
using PatchKit.Network;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    public class RemoteMetaData : IRemoteMetaData
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(RemoteMetaData));

        private readonly string _appSecret;
        private readonly MainApiConnection _mainApiConnectionWithoutRetry;
        private readonly MainApiConnection _mainApiConnection;
        private readonly KeysApiConnection _keysApiConnection;

        public RemoteMetaData([NotNull] string appSecret, [NotNull] IRequestTimeoutCalculator requestTimeoutCalculator)
        {
            if (string.IsNullOrEmpty(appSecret))
                throw new ArgumentException("Value cannot be null or empty.", "appSecret");
            if (requestTimeoutCalculator == null) throw new ArgumentNullException("requestTimeoutCalculator");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(appSecret, "appSecret");

            _appSecret = appSecret;

            var mainSettings = Settings.GetMainApiConnectionSettings();

            _mainApiConnection = new MainApiConnection(mainSettings)
            {
                HttpClient = new UnityHttpClient(),
                RequestTimeoutCalculator = requestTimeoutCalculator,
                RequestRetryStrategy = new SimpleInfiniteRequestRetryStrategy(),
                Logger = PatcherLogManager.DefaultLogger
            };

            _mainApiConnectionWithoutRetry = new MainApiConnection(mainSettings)
            {
                HttpClient = new UnityHttpClient(),
                RequestTimeoutCalculator = requestTimeoutCalculator,
                Logger = PatcherLogManager.DefaultLogger
            };

            var keysSettings = Settings.GetKeysApiConnectionSettings();

            _keysApiConnection = new KeysApiConnection(keysSettings)
            {
                HttpClient = new UnityHttpClient(),
                RequestTimeoutCalculator = requestTimeoutCalculator,
                Logger = PatcherLogManager.DefaultLogger
            };
        }

        public int GetLatestVersionId(bool retryRequests = !false)
        {
            DebugLogger.Log("Getting latest version id.");
            DebugLogger.Log("retryRequests = " + retryRequests);
            var m = retryRequests ? _mainApiConnection : _mainApiConnectionWithoutRetry;
#pragma warning disable 612
            return m.GetAppLatestAppVersionId(_appSecret).Id;
#pragma warning restore 612
        }

        public Api.Models.Main.App GetAppInfo(bool retryRequests = !false)
        {
            DebugLogger.Log("Getting app info.");
            DebugLogger.Log("retryRequests = " + retryRequests);
            var m = retryRequests ? _mainApiConnection : _mainApiConnectionWithoutRetry;
            return m.GetApplicationInfo(_appSecret);
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

        public AppVersion GetAppVersionInfo(
            int versionId,
            bool retryRequests = !false)
        {
            if (versionId <= 0)
            {
                throw new ArgumentException("Version id is invalid.", "versionId");
            }

            DebugLogger.Log(string.Format("Getting app version info for version with id {0}", versionId));

            var m = retryRequests ? _mainApiConnection : _mainApiConnectionWithoutRetry;

            return m.GetAppVersion(_appSecret, versionId);
        }
    }
}