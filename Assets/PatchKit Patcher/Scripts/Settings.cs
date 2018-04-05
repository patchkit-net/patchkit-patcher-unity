using System;
using JetBrains.Annotations;
using PatchKit.Api;
using PatchKit.Apps.Updating.AppData.Remote;
using PatchKit.Apps.Updating.Debug;
using UnityEngine;

namespace PatchKit.Patching.Unity
{
    public class Settings : ScriptableObject, IApiConnectionSettingsProvider
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
            settings.MainApiConnectionSettings = MainApiConnection.GetDefaultSettings();
            settings.KeysApiConnectionSettings = KeysApiConnection.GetDefaultSettings();

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

                return new ApiConnectionServer
                {
                    Host = uri.Host,
                    Port = uri.Port,
                    UseHttps = uri.Scheme == Uri.UriSchemeHttps
                };
            }

            return null;
        }

        public ApiConnectionSettings GetMainApiSettings()
        {
            var settings = MainApiConnectionSettings;

            var overrideMain = GetApiConnectionServerFromEnvVar(EnvironmentVariables.ApiUrlEnvironmentVariable);

            if (overrideMain.HasValue)
            {
                settings.MainServer = overrideMain.Value;
            }

            var overrideMainCache =
                GetApiConnectionServerFromEnvVar(EnvironmentVariables.ApiCacheUrlEnvironmentVariable);

            if (overrideMainCache.HasValue)
            {
                settings.CacheServers = new[] {overrideMainCache.Value};
            }

            return settings;
        }

        public ApiConnectionSettings GetKeysApiSettings()
        {
            var settings = KeysApiConnectionSettings;

            var overrideKeys = GetApiConnectionServerFromEnvVar(EnvironmentVariables.KeysUrlEnvironmentVariable);

            if (overrideKeys.HasValue)
            {
                settings.MainServer = overrideKeys.Value;
            }

            return settings;
        }
    }
}