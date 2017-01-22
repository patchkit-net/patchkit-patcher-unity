using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PatchKit.Unity.Patcher
{
    internal class PatcherConfigurationParser
    {
        public void Parse(ref PatcherConfiguration configuration)
        {
            string forceAppSecret = Environment.GetEnvironmentVariable("PK_PATCHER_FORCE_SECRET");

            if (forceAppSecret != null)
            {
                configuration.AppSecret = forceAppSecret;
                Debug.Log(string.Format("Using forced app secret from environment variable PK_PATCHER_FORCE_SECRET - {0}", forceAppSecret));
            }
            else
            {
                string appSecret;

                if (TryReadArgument("--secret", out appSecret))
                {
                    configuration.AppSecret = DecodeSecret(appSecret);
                }
            }

            string applicationDataPath;

            if (TryReadArgument("--installdir", out applicationDataPath))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                configuration.ApplicationDataPath = Path.Combine(GetBaseApplicationDataPath(), applicationDataPath);
            }

            string forceAppVersionString = Environment.GetEnvironmentVariable("PK_PATCHER_FORCE_VERSION");

            if (forceAppVersionString != null)
            {
                int forceAppVersion;

                if (int.TryParse(forceAppVersionString, out forceAppVersion))
                {
                    configuration.ForceAppVersion = forceAppVersion;
                    Debug.Log(string.Format("Using forced app version from environment variable PK_PATCHER_FORCE_VERSION - {0}", forceAppVersion));
                }
            }
        }

        private static string GetBaseApplicationDataPath()
        {
            string path = Path.GetDirectoryName(Application.dataPath);

            if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                path = Path.GetDirectoryName(path);
            }

            return path;
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