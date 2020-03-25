using System;

namespace PatchKit.Unity.Patcher.Debug
{
    public static class EnvironmentInfo
    {
        public static string GetRuntimeVersion()
        {
            return Environment.Version.ToString();
        }

        public static string GetSystemVersion()
        {
            return Environment.OSVersion.ToString();
        }

        public static string GetSystemInformation()
        {
            return UnityEngine.SystemInfo.operatingSystem;
        }

        public static bool TryReadEnvironmentVariable(string argumentName, out string value)
        {
            value = Environment.GetEnvironmentVariable(argumentName);

            return value != null;
        }

        public static string GetEnvironmentVariable(string name, string @default)
        {
            string value;
            if (TryReadEnvironmentVariable(name, out value))
            {
                return value;
            }

            return @default;
        }
    }
}