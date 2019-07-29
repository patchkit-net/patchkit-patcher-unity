using System;
using System.IO;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.FileSystem;
using PatchKit.Unity.Patcher.Cancellation;
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
                DirectoryOperations.CreateDirectory(dirName, CancellationToken.Empty);
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

        public static void WriteAllText(string fileName, string text) {
            new Retry().Times(10).IntervalSeconds(0.5f).Run(() => {
                File.WriteAllText(fileName, text);
            });
        }
    }
}