using System;
using System.IO;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// Implementation of <see cref="ITemporaryDirectory"/>.
    /// </summary>
    /// <seealso cref="BaseWritableDirectory{TemporaryDirectory}" />
    /// <seealso cref="ITemporaryDirectory" />
    public class TemporaryDirectory : BaseWritableDirectory<TemporaryDirectory>, ITemporaryDirectory
    {
        private bool _disposed;

        public TemporaryDirectory(string path) : base(path)
        {
        }

        public string GetUniquePath()
        {
            AssertChecks.IsFalse(_disposed, "Object has been disposed.");

            string uniquePath = string.Empty;

            for (int i = 0; i < 1000; i++)
            {
                // Use first function if second would cause problems.
                //path = System.IO.Path.Combine(Path, Guid.NewGuid().ToString("N"));
                uniquePath = Path.PathCombine(System.IO.Path.GetRandomFileName());
                if (!File.Exists(uniquePath) && !Directory.Exists(uniquePath))
                {
                    break;
                }
            }

            return uniquePath;
        }

        public override void PrepareForWriting()
        {
            AssertChecks.IsFalse(_disposed, "Object has been disposed.");

            base.PrepareForWriting();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TemporaryDirectory()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            DebugLogger.LogDispose();

            if (Directory.Exists(Path))
            {
                DirectoryOperations.Delete(Path, true);
            }

            _disposed = true;
        }
    }
}