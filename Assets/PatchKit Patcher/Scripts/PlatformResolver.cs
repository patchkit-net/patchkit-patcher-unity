using System.Linq;
using PatchKit.Apps.Updating.Utilities;
using UnityEngine;

namespace PatchKit.Patching.Unity
{
    public class PlatformResolver : IPlatformResolver
    {
        public static RuntimePlatform GetRuntimePlatform()
        {
            return Application.platform;
        }

        public PlatformType GetPlatformType()
        {
            if (IsWindows())
            {
                return PlatformType.Windows;
            }
            if (IsOSX())
            {
                return PlatformType.OSX;
            }
            if (IsLinux())
            {
                return PlatformType.Linux;
            }
            return PlatformType.Unknown;
        }

        public static bool IsWindows()
        {
            return IsOneOf(RuntimePlatform.WindowsPlayer, RuntimePlatform.WindowsEditor);
        }

        public static bool IsOSX()
        {
            return IsOneOf(RuntimePlatform.OSXPlayer, RuntimePlatform.OSXEditor);
        }

        public static bool IsLinux()
        {
            // use preprocessor for this check due to Unity bug in platform enum
            // it is missing LinuxEditor entry
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            return true;
#else
            return false;
#endif
        }

        private static bool IsOneOf(params RuntimePlatform[] platforms)
        {
            var runtimePlatform = GetRuntimePlatform();
            return platforms.Any(platform => platform == runtimePlatform);
        }
    }
}