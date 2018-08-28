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

        public static bool TryReadBoolEnvironmentVariable(string varName, out bool value)
        {
            string varValue = Environment.GetEnvironmentVariable(varName);

            if (string.IsNullOrEmpty(varValue))
            {
                value = false;
                return false;
            }

            switch (varValue.ToLower())
            {
                case "true":
                case "yes":
                case "1":
                    value = true;
                    return true;

                case "false":
                case "no":
                case "0":
                    value = false;
                    return true;

                default:
                    value = false;
                    return false;

            }
        }
    }
}