using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher
{
    public class InputArgumentsPatcherDataReader
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(InputArgumentsPatcherDataReader));
        private static readonly List<string> _commandLineArgs = Environment.GetCommandLineArgs().ToList();

        public InputArgumentsPatcherDataReader()
        {
            DebugLogger.LogConstructor();
        }

        public PatcherData Read()
        {
            DebugLogger.Log("Reading.");

            PatcherData data = new PatcherData();

            if (!HasArgument("--secret") || !HasArgument("--installdir"))
            {
                DebugLogger.Log("Expected the secret and installdir to be present in the command line arguments.");
                throw new NonLauncherExecutionException("Patcher has been started without a Launcher.");
            }

            string forceAppSecret;
            if (EnvironmentInfo.TryReadEnvironmentVariable(EnvironmentVariables.ForceSecretEnvironmentVariable, out forceAppSecret))
            {
                DebugLogger.LogFormat("Setting forced app secret {0}", forceAppSecret);
                data.AppSecret = forceAppSecret;
            }
            else
            {
                string appSecret;

                if (!TryReadArgument("--secret", out appSecret))
                {
                    throw new ApplicationException("Unable to parse secret from command line.");
                }
                data.AppSecret = IsReadable() ? appSecret : DecodeSecret(appSecret);
            }

            string forceOverrideLatestVersionIdString;
            if (EnvironmentInfo.TryReadEnvironmentVariable(EnvironmentVariables.ForceVersionEnvironmentVariable, out forceOverrideLatestVersionIdString))
            {
                int forceOverrideLatestVersionId;

                if (int.TryParse(forceOverrideLatestVersionIdString, out forceOverrideLatestVersionId))
                {
                    DebugLogger.LogFormat("Setting forced version id {0}", forceOverrideLatestVersionId);
                    data.OverrideLatestVersionId = forceOverrideLatestVersionId;
                }
            }
            else
            {
                data.OverrideLatestVersionId = 0;
            }

            string relativeAppDataPath;

            if (!TryReadArgument("--installdir", out relativeAppDataPath))
            {
                throw new ApplicationException("Unable to parse app data path from command line.");
            }
            data.AppDataPath = MakeAppDataPathAbsolute(relativeAppDataPath);

            string lockFilePath;
            if (TryReadArgument("--lockfile", out lockFilePath))
            {
                data.LockFilePath = lockFilePath;
                DebugLogger.LogFormat("Using lock file: {0}", lockFilePath);
            }
            else
            {
                DebugLogger.LogWarning("Lock file not provided.");
            }

            if (HasArgument("--online"))
            {
                data.IsOnline = true;
            }
            else if (HasArgument("--offline"))
            {
                data.IsOnline = false;
            }
            else
            {
                data.IsOnline = null;
            }

            return data;
        }

        private static string MakeAppDataPathAbsolute(string relativeAppDataPath)
        {
            string path = Path.GetDirectoryName(Application.dataPath);

            if (Platform.GetRuntimePlatform() == RuntimePlatform.OSXPlayer)
            {
                path = Path.GetDirectoryName(path);
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            return Path.Combine(path, relativeAppDataPath);
        }

        private static bool TryReadArgument(string argumentName, out string value)
        {
            int index = _commandLineArgs.IndexOf(argumentName);

            if (index != -1 && index < _commandLineArgs.Count - 1)
            {
                value = _commandLineArgs[index + 1];

                return true;
            }

            value = null;

            return false;
        }

        private static bool IsReadable()
        {
            return HasArgument("--readable");
        }

        private static bool HasArgument(string argumentName)
        {
            return _commandLineArgs.Contains(argumentName);
        }

        private static string DecodeSecret(string encodedSecret)
        {
            var bytes = Convert.FromBase64String(encodedSecret);

            for (int i = 0; i < bytes.Length; ++i)
            {
                byte b = bytes[i];
                bool lsb = (b & 1) > 0;
                b >>= 1;
                b |= (byte) (lsb ? 128 : 0);
                b = (byte) ~b;
                bytes[i] = b;
            }

            var chars = new char[bytes.Length/sizeof(char)];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
    }
}