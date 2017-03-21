using System;
using System.IO;
using System.Linq;
using PatchKit.Unity.Patcher.Data;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher
{
    public class AppFinder
    {
        public string FindExecutable(string path, PlatformType platformType)
        {
            switch (platformType)
            {
                case PlatformType.Unknown:
                    throw new ArgumentException("Unknown platform");
                case PlatformType.Windows:
                    return FindWindowsExecutable(path);
                case PlatformType.OSX:
                    return FindOSXExectutable(path);
                case PlatformType.Linux:
                    return FindLinuxExecutable(path);
                default:
                    throw new ArgumentOutOfRangeException("platformType", platformType, null);
            }
        }

        public string FindWindowsExecutable(string path)
        {
            return FindInFilesRecursively(path, s => s.EndsWith(".exe"));
        }

        public string FindOSXExectutable(string path)
        {
            return FindInFilesRecursively(path, s => s.EndsWith(".app"));
        }

        public string FindLinuxExecutable(string path)
        {
            return FindInFilesRecursively(path, MagicBytes.IsLinuxExecutable);
        }

        private string FindInFilesRecursively(string path, Func<string, bool> predicate)
        {
            return Directory.GetFileSystemEntries(path).First(predicate);
        }
    }
}