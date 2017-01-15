using System;
using System.IO;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class TemporaryDirectory : IDisposable
    {
        public readonly string Path;

        public TemporaryDirectory(string path)
        {
            Checks.ArgumentNotNullOrEmpty(path, "path");

            Path = path;

            if (!Directory.Exists(Path))
            {
                Directory.CreateDirectory(Path);
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
