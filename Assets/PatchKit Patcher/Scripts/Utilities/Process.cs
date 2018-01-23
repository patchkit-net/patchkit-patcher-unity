using System.IO;
using System.Diagnostics;

namespace PatchKit.Unity.Utilities
{
    public static class ProcessUtils
    {
        public static Process Launch(string target)
        {
            var startInfo = Prepare(target);
            return Process.Start(startInfo);
        }
    
        public static ProcessStartInfo Prepare(string target)
        {
            var platformType = Platform.GetPlatformType();

            if (platformType == PlatformType.OSX)
            {
                return PrepareOSX(target);
            }

            return new ProcessStartInfo
            {
                FileName = target
            };
        }

        private static ProcessStartInfo PrepareOSX(string target)
        {
            return new ProcessStartInfo
            {
                FileName = "open",
                Arguments = string.Format("\"{0}\"", target)
            };
        }
    }
}