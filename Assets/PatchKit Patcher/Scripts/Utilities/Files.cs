using System;
using System.IO;
using PatchKit.Unity.Patcher.Data;

namespace PatchKit.Unity.Utilities
{
    public class Files
    {
        public static void CreateParents(string path)
        {
            var dirName = Path.GetDirectoryName(path);
            if (dirName != null)
            {
                Directory.CreateDirectory(dirName);
            }
        }

        public static bool IsExecutable(string filePath, PlatformType platformType)
        {
            switch (platformType)
            {
                case PlatformType.Unknown:
                    throw new ArgumentException("Unknown");
                case PlatformType.Windows:
                    return filePath.EndsWith(".exe");
                case PlatformType.OSX:
                    return MagicBytes.IsMacExecutable(filePath);
                case PlatformType.Linux:
                    return MagicBytes.IsLinuxExecutable(filePath);
                default:
                    throw new ArgumentOutOfRangeException("platformType", platformType, null);
            }
        }
    }
}