using System;
using Debugging;
using JetBrains.Annotations;
using PatchKit.Api;
using PatchKit.Core.CSharp;
using UnityEngine;

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
        settings.MainApiConnectionSettings = PatchKit.Api.Properties.AssemblyModule.DefaultApiConnectionSettings;
        settings.KeysApiConnectionSettings = PatchKit.Api.Properties.AssemblyModule.DefaultKeysApiConnectionSettings;

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

            return new ApiConnectionServer(
                host: uri.Host,
                port: uri.Port,
                useHttps: uri.Scheme == Uri.UriSchemeHttps);
        }

        return null;
    }

    public static ApiConnectionSettings GetMainApiConnectionSettings()
    {
        var instance = FindInstance();

        var settings = instance == null
            ? PatchKit.Api.Properties.AssemblyModule.DefaultApiConnectionSettings
            : instance.MainApiConnectionSettings;

        var overrideMain = GetApiConnectionServerFromEnvVar(EnvironmentVariables.ApiUrlEnvironmentVariable);

        if (overrideMain.HasValue)
        {
            settings = new ApiConnectionSettings(
                mainServer: overrideMain.Value,
                cacheServers: settings.CacheServers);
        }

        var overrideMainCache =
            GetApiConnectionServerFromEnvVar(EnvironmentVariables.ApiCacheUrlEnvironmentVariable);

        if (overrideMainCache.HasValue)
        {
            settings = new ApiConnectionSettings(
                mainServer: settings.MainServer,
                cacheServers: new[] {overrideMainCache.Value}.ToImmutableArray());
        }

        return settings;
    }

    public static ApiConnectionSettings GetKeysApiConnectionSettings()
    {
        var instance = FindInstance();

        var settings = instance == null
            ? PatchKit.Api.Properties.AssemblyModule.DefaultKeysApiConnectionSettings
            : instance.KeysApiConnectionSettings;

        var overrideKeys = GetApiConnectionServerFromEnvVar(EnvironmentVariables.KeysUrlEnvironmentVariable);

        if (overrideKeys.HasValue)
        {
            settings = new ApiConnectionSettings(
                mainServer: overrideKeys.Value,
                cacheServers: settings.CacheServers);
        }

        return settings;
    }
}