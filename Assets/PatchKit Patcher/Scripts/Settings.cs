using System;
using Debugging;
using JetBrains.Annotations;
using PatchKit.Api;
using PatchKit.Core.CSharp;
using UnityEngine;

public class Settings
{
    private static ApiConnectionServer? GetApiConnectionServerFromEnvVar(
        string argumentName)
    {
        string url;

        if (EnvironmentInfo.TryReadEnvironmentVariable(
            argumentName,
            out url))
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
        var settings = PatchKit.Api.Properties.AssemblyModule
            .DefaultApiConnectionSettings;

        var overrideMain = GetApiConnectionServerFromEnvVar(
            EnvironmentVariables.ApiUrlEnvironmentVariable);

        if (overrideMain.HasValue)
        {
            settings = new ApiConnectionSettings(
                mainServer: overrideMain.Value,
                cacheServers: settings.CacheServers);
        }

        var overrideMainCache = GetApiConnectionServerFromEnvVar(
            EnvironmentVariables.ApiCacheUrlEnvironmentVariable);

        if (overrideMainCache.HasValue)
        {
            settings = new ApiConnectionSettings(
                mainServer: settings.MainServer,
                cacheServers: new[]
                {
                    overrideMainCache.Value
                }.ToImmutableArray());
        }

        return settings;
    }

    public static ApiConnectionSettings GetKeysApiConnectionSettings()
    {
        var settings = PatchKit.Api.Properties.AssemblyModule
            .DefaultKeysApiConnectionSettings;

        var overrideKeys = GetApiConnectionServerFromEnvVar(
            EnvironmentVariables.KeysUrlEnvironmentVariable);

        if (overrideKeys.HasValue)
        {
            settings = new ApiConnectionSettings(
                mainServer: overrideKeys.Value,
                cacheServers: settings.CacheServers);
        }

        return settings;
    }
}