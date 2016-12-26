using System;
using System.IO;

namespace PatchKit.Unity.Patcher.Data.Local
{
    internal class TemporaryDirectory : IDisposable
    {
        public readonly string Path;

        public TemporaryDirectory(string path)
        {
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
