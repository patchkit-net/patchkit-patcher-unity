using System;
using JetBrains.Annotations;
using PatchKit.Api;
using PatchKit.Apps.Updating.AppData.Remote;
using PatchKit.Apps.Updating.Debug;
using PatchKit.Core.Collections.Immutable;
using UnityEngine;

namespace PatchKit.Patching.Unity
{
    public class Settings : ScriptableObject
    {
        private const string AssetFileName = "PatchKit Settings";

        [SerializeField] public ApiConnectionSettings MainApiConnectionSettings;

        [SerializeField] public ApiConnectionSettings KeysApiConnectionSettings;

#if UNITY_EDITOR
        private static Settings CreateSettingsInstance()
        {
            bool pingObject = false;

            if (UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.EditorApplication.isPaused = true;

                UnityEditor.EditorUtility.DisplayDialog("PatchKit Settings has been created.",
                    "PatchKit Settings asset has been created.", "OK");

                pingObject = true;
            }

            var settings = CreateInstance<Settings>();
            settings.MainApiConnectionSettings = ApiConnectionSettings.DefaultApi;
            settings.KeysApiConnectionSettings = ApiConnectionSettings.DefaultKeysApi;

            UnityEditor.AssetDatabase.CreateAsset(settings,
                string.Format("Assets/PatchKit Patcher/Resources/{0}.asset", AssetFileName));
            UnityEditor.EditorUtility.SetDirty(settings);

            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.AssetDatabase.SaveAssets();

            if (pingObject)
            {
                UnityEditor.EditorGUIUtility.PingObject(settings);
            }

            return settings;
        }
#endif

        [CanBeNull]
        public static Settings FindInstance()
        {
            var settings = Resources.Load<Settings>(AssetFileName);

#if UNITY_EDITOR
            if (settings == null)
            {
                settings = CreateSettingsInstance();
            }
#endif
            return settings;
        }

        private static ApiConnectionServer? GetApiConnectionServerFromEnvVar(string argumentName)
        {
            string url;

            if (EnvironmentInfo.TryReadEnvironmentVariable(argumentName, out url))
            {
                var uri = new Uri(url);

                return new ApiConnectionServer(uri.Host, uri.Port, uri.Scheme == Uri.UriSchemeHttps);
            }

            return null;
        }

        public ApiConnectionSettings GetMainApiSettings()
        {
            ApiConnectionSettings settings = MainApiConnectionSettings;

            var overrideMain = GetApiConnectionServerFromEnvVar(EnvironmentVariables.ApiUrlEnvironmentVariable);

            var overrideMainCache = GetApiConnectionServerFromEnvVar(EnvironmentVariables.ApiCacheUrlEnvironmentVariable);

            ImmutableArray<ApiConnectionServer>? overrideCache = null;
            
            if (overrideMainCache.HasValue) 
            {
                overrideCache = (new[] {overrideMainCache.Value}).ToImmutableArray();
            }

            return new ApiConnectionSettings(
                mainServer: overrideMain ?? settings.MainServer,
                cacheServers: overrideCache ?? settings.CacheServers
            );
        }

        public ApiConnectionSettings GetKeysApiSettings()
        {
            var settings = KeysApiConnectionSettings;

            var overrideKeys = GetApiConnectionServerFromEnvVar(EnvironmentVariables.KeysUrlEnvironmentVariable);

            return new ApiConnectionSettings(
                mainServer: overrideKeys ?? settings.MainServer,
                cacheServers: settings.CacheServers
            );
        }
    }
}