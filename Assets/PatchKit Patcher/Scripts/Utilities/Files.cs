using System;
using System.IO;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.Data;

namespace PatchKit.Unity.Utilities
{
    public static class Files
    {
        public static void CreateParents(string path)
        {
            var dirName = Path.GetDirectoryName(path);
            if (dirName != null)
            {
                DirectoryOperations.CreateDirectory(dirName);
            }
        }

        public static bool IsExecutable(string filePath, PlatformType platformType)
        {
            switch (platformType)
            {
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