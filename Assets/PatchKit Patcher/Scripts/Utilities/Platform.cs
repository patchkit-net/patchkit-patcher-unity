using System.Linq;
using UnityEngine;

namespace PatchKit.Unity.Utilities
{
    public class Platform
    {
        public static PlatformResolver PlatformResolver { get; set; }

        static Platform()
        {
            PlatformResolver = new PlatformResolver();
        }

        public static RuntimePlatform GetRuntimePlatform()
        {
            return PlatformResolver.GetRuntimePlatform();
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
            // TODO: Why there's no Linux Editor?
            return IsOneOf(RuntimePlatform.LinuxPlayer);
        }

        public static bool IsOneOf(params RuntimePlatform[] platforms)
        {
            var runtimePlatform = GetRuntimePlatform();
            return platforms.Any(platform => platform == runtimePlatform);
        }
    }

    public class PlatformResolver
    {
        public RuntimePlatform GetRuntimePlatform()
        {
            return Application.platform;
        }
    }
}