using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PatchKit.Apps.Updating;
using PatchKit.Apps.Updating.Debug;
using PatchKit.Logging;
using UnityEngine;
using ILogger = PatchKit.Logging.ILogger;

namespace PatchKit.Patching.Unity
{
    public class InputArgumentsPatcherDataReader
    {
        private readonly ILogger _logger;
        private static readonly List<string> CommandLineArgs = Environment.GetCommandLineArgs().ToList();

        public InputArgumentsPatcherDataReader()
        {
            _logger = DependencyResolver.Resolve<ILogger>();
        }

        public PatcherData Read()
        {
            _logger.LogDebug("Reading.");

            PatcherData data = new PatcherData();

            if (!HasArgument("--secret") || !HasArgument("--installdir"))
            {
                _logger.LogDebug("Expected the secret and installdir to be present in the command line arguments.");
                throw new NonLauncherExecutionException("Patcher has been started without a Launcher.");
            }

            string forceAppSecret;
            if (EnvironmentInfo.TryReadEnvironmentVariable(EnvironmentVariables.ForceSecretEnvironmentVariable, out forceAppSecret))
            {
                _logger.LogDebug($"Setting forced app secret {forceAppSecret}");
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
                    _logger.LogDebug($"Setting forced version id {forceOverrideLatestVersionId}");
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
                _logger.LogDebug($"Using lock file: {lockFilePath}");
            }
            else
            {
                _logger.LogWarning("Lock file not provided.");
            }

            return data;
        }

        private static string MakeAppDataPathAbsolute(string relativeAppDataPath)
        {
            string path = Path.GetDirectoryName(Application.dataPath);

            if (PlatformResolver.GetRuntimePlatform() == RuntimePlatform.OSXPlayer)
            {
                path = Path.GetDirectoryName(path);
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            return Path.Combine(path, relativeAppDataPath);
        }

        private static bool TryReadArgument(string argumentName, out string value)
        {
            int index = CommandLineArgs.IndexOf(argumentName);

            if (index != -1 && index < CommandLineArgs.Count - 1)
            {
                value = CommandLineArgs[index + 1];

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
            return CommandLineArgs.Contains(argumentName);
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