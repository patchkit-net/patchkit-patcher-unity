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

        public static bool TryReadEnvironmentVariable(string argumentName, out string value)
        {
            value = Environment.GetEnvironmentVariable(argumentName);

            return value != null;
        }
    }
}