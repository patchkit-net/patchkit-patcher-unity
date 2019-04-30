using System;
using System.IO;
using System.Linq;
using Debugging;
using UnityEngine;

public partial class Patcher
{
    private static InitializationData? LoadCommandLineInitializationData()
    {
        Debug.Log(message: "Loading initialization data from command line...");

        var args = Environment.GetCommandLineArgs().ToList();

        Debug.Log(
            message:
            $"Command line args: {string.Join(separator: " ", values: args)}");

        bool areReadable = args.Contains(item: "--readable");

        string appSecret = null;
        string appPath = null;
        bool? isOnline = null;
        string lockFilePath = null;
        int? overrideAppLatestVersionId = null;

        for (int i = 0; i < args.Count; i++)
        {
            if (i + 1 < args.Count)
            {
                if (args[index: i] == "--lockfile")
                {
                    lockFilePath = args[index: i + 1];

                    i++;
                    continue;
                }

                if (args[index: i] == "--installdir")
                {
                    appPath = MakeAppPathAbsolute(
                        relativeAppDataPath: args[index: i + 1]);

                    i++;
                    continue;
                }

                if (args[index: i] == "--secret")
                {
                    appSecret = areReadable
                        ? args[index: i + 1]
                        : DecodeSecret(encodedSecret: args[index: i + 1]);

                    i++;
                    continue;
                }
            }

            if (args[index: i] == "--online")
            {
                isOnline = true;
            }
            else if (args[index: i] == "--offline")
            {
                isOnline = false;
            }
        }

        string forceAppSecret;
        if (EnvironmentInfo.TryReadEnvironmentVariable(
            argumentName: EnvironmentVariables.ForceSecretEnvironmentVariable,
            value: out forceAppSecret))
        {
            Debug.Log(
                message:
                $"Using app secret from environment variable: {EnvironmentVariables.ForceSecretEnvironmentVariable}={forceAppSecret}");

            appSecret = forceAppSecret;
        }

        string forceOverrideLatestVersionIdString;
        if (EnvironmentInfo.TryReadEnvironmentVariable(
            argumentName: EnvironmentVariables.ForceVersionEnvironmentVariable,
            value: out forceOverrideLatestVersionIdString))
        {
            int forceOverrideLatestVersionId;

            if (int.TryParse(
                s: forceOverrideLatestVersionIdString,
                result: out forceOverrideLatestVersionId))
            {
                Debug.Log(
                    message:
                    $"Using override latest version id from environment variable: {EnvironmentVariables.ForceVersionEnvironmentVariable}={forceOverrideLatestVersionId}");


                overrideAppLatestVersionId = forceOverrideLatestVersionId;
            }
        }

        if (appSecret == null || appPath == null)
        {
            return null;
        }

        var result = new InitializationData
        {
            AppPath = appPath,
            AppSecret = appSecret,
            IsOnline = isOnline,
            LockFilePath = lockFilePath,
            OverrideAppLatestVersionId = overrideAppLatestVersionId
        };

        Debug.Log(message: "Initialization data loaded.");

        return result;
    }

    private static string MakeAppPathAbsolute(string relativeAppDataPath)
    {
        if (relativeAppDataPath == null)
        {
            return null;
        }

        string path = Path.GetDirectoryName(path: Application.dataPath);

        if (Application.platform == RuntimePlatform.OSXPlayer)
        {
            path = Path.GetDirectoryName(path: path);
        }

        // ReSharper disable once AssignNullToNotNullAttribute
        return Path.Combine(
            path1: path,
            path2: relativeAppDataPath);
    }

    private static string DecodeSecret(string encodedSecret)
    {
        if (encodedSecret == null)
        {
            return null;
        }

        var bytes = Convert.FromBase64String(s: encodedSecret);

        for (int i = 0; i < bytes.Length; ++i)
        {
            byte b = bytes[i];
            bool lsb = (b & 1) > 0;
            b >>= 1;
            b |= (byte) (lsb ? 128 : 0);
            b = (byte) ~b;
            bytes[i] = b;
        }

        var chars = new char[bytes.Length / sizeof(char)];
        Buffer.BlockCopy(
            src: bytes,
            srcOffset: 0,
            dst: chars,
            dstOffset: 0,
            count: bytes.Length);

        return new string(value: chars);
    }
}