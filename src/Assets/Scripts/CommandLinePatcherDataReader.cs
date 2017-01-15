using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PatchKit.Unity.Patcher
{
    public class CommandLinePatcherDataReader
    {
        public PatcherData Read()
        {
            PatcherData data;

            string encodedAppSecret;

            if (!TryReadArgument("--secret", out encodedAppSecret))
            {
                throw new ApplicationException("Unable to parse secret from command line.");
            }

            data.AppSecret = DecodeSecret(encodedAppSecret);

            string relativeAppDataPath;

            if (!TryReadArgument("--installDir", out relativeAppDataPath))
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