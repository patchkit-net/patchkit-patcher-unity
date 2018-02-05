using System;
using System.IO;
using JetBrains.Annotations;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// Implementation of <see cref="ITemporaryDirectory"/>.
    /// </summary>
    /// <seealso cref="BaseWritableDirectory{TemporaryDirectory}" />
    /// <seealso cref="ITemporaryDirectory" />
    public sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; private set; }

        public TemporaryDirectory([NotNull] string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Value cannot be null or empty.", "path");
            }

            Path = path;

            Directory.CreateDirectory(Path);
        }

        //TODO: Move it to some extension method.
        public string GetUniquePath()
        {
            string uniquePath = string.Empty;

            for (int i = 0; i < 1000; i++)
            {
                // Use first function if second would cause problems.
                //path = System.IO.Path.Combine(Path, Guid.NewGuid().ToString("N"));
                uniquePath = Path.PathCombine(System.IO.Path.GetRandomFileName());
                if (!File.Exists(uniquePath) && !Directory.Exists(uniquePath))
                {
                    return uniquePath;
                }
            }

            throw new Exception("Cannot find unique path.");
        }

        private void ReleaseUnmanagedResources()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~TemporaryDirectory()
        {
            ReleaseUnmanagedResources();
        }
    }
}