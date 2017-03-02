using System;
using System.IO;
using System.Linq;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine;

namespace PatchKit.Unity.Patcher
{
    public class CommandLinePatcherDataReader
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(CommandLinePatcherDataReader));

        public CommandLinePatcherDataReader()
        {
            DebugLogger.LogConstructor();
        }

        public PatcherData Read()
        {
            DebugLogger.Log("Reading.");

            PatcherData data = new PatcherData();

            string forceAppSecret;
            if (TryReadDebugArgument("PK_PATCHER_FORCE_SECRET", out forceAppSecret))
            {
                DebugLogger.Log(string.Format("Setting forced app secret {0}", forceAppSecret));
                data.AppSecret = forceAppSecret;
            }
            else
            {
                string encodedAppSecret;

                if (!TryReadArgument("--secret", out encodedAppSecret))
                {
                    throw new ApplicationException("Unable to parse secret from command line.");
                }
                data.AppSecret = DecodeSecret(encodedAppSecret);
            }

            string forceOverrideLatestVersionIdString;
            if (TryReadDebugArgument("PK_PATCHER_FORCE_VERSION", out forceOverrideLatestVersionIdString))
            {
                int forceOverrideLatestVersionId;

                if (int.TryParse(forceOverrideLatestVersionIdString, out forceOverrideLatestVersionId))
                {
                    DebugLogger.Log(string.Format("Setting forced version id {0}", forceOverrideLatestVersionId));
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

            return data;
        }

        private static string MakeAppDataPathAbsolute(string relativeAppDataPath)
        {
            string path = Path.GetDirectoryName(Application.dataPath);

            if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                path = Path.GetDirectoryName(path);
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            return Path.Combine(path, relativeAppDataPath);
        }

        private static bool TryReadArgument(string argumentName, out string value)
        {
            var args = Environment.GetCommandLineArgs().ToList();

            int index = args.IndexOf(argumentName);

            if (index != -1 && index < args.Count - 1)
            {
                value = args[index + 1];

                return true;
            }

            value = null;

            return false;
        }

        private static bool TryReadDebugArgument(string argumentName, out string value)
        {
            value = Environment.GetEnvironmentVariable(argumentName);

            return value != null;
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