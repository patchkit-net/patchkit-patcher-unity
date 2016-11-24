using System;
using System.IO;

namespace PatchKit.Unity.Patcher.Data
{
    public class TemporaryDirectory : IDisposable
    {
        public string Path { get; private set; }

        public static TemporaryDirectory CreateDefault()
        {
            string path;

            int iteration = 0;

            do
            {
                path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
            } while (iteration++ < 1000 && Directory.Exists(path));

            return new TemporaryDirectory(path);
        }

        public TemporaryDirectory(string path)
        {
            Path = path;
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }
}