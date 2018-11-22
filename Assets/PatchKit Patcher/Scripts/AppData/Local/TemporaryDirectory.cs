using System;
using System.IO;
using JetBrains.Annotations;
using PatchKit.Unity.Patcher.AppData.FileSystem;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

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

        private bool _keep = false;

        private TemporaryDirectory([NotNull] string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Value cannot be null or empty.", "path");
            }

            Path = path;

            if (Directory.Exists(Path))
            {
                DirectoryOperations.Delete(Path, CancellationToken.Empty, true);
            }

            DirectoryOperations.CreateDirectory(Path, CancellationToken.Empty);
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

        public void Keep()
        {
            _keep = true;
        }

        private void ReleaseUnmanagedResources()
        {
            if (!_keep && Directory.Exists(Path))
            {
                DirectoryOperations.Delete(Path, CancellationToken.Empty, true);
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

        public static void ExecuteIn(string tempDirName, Action<TemporaryDirectory> action)
        {
            using (var tempDir = new TemporaryDirectory(tempDirName))
            {
                try
                {
                    action(tempDir);
                }
                catch (Exception)
                {
                    if (ShouldKeepFilesOnError())
                    {
                        tempDir.Keep();
                    }
                    throw;
                }
            }
        }

        private static bool ShouldKeepFilesOnError()
        {
            string value = null;

            if (EnvironmentInfo.TryReadEnvironmentVariable(EnvironmentVariables.KeepFilesOnErrorEnvironmentVariable, out value))
            {
                return !(string.IsNullOrEmpty(value) || value == "0");
            }

            return false;
        }
    }
}
