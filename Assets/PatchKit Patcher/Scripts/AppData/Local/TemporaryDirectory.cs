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
        private string _prefix;
        private DateTime _createdAt;

        public TemporaryDirectory(string path, string prefix) : base(path.PathCombine(prefix + "_" + System.IO.Path.GetRandomFileName()))
        {
            Checks.ArgumentNotNullOrEmpty(prefix, "prefix");

            _prefix = prefix;
            _createdAt = DateTime.Now;
        }

        public TemporaryDirectory(string path) : base(path)
        {
        }

        public string GetUniquePath()
        {
            Assert.IsFalse(_disposed, "Object has been disposed.");

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
            Assert.IsFalse(_disposed, "Object has been disposed.");

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

            DebugLogger.Log("TemporaryDirectory: Deleting: " + Path);
            if (Directory.Exists(Path))
            {
                DirectoryOperations.Delete(Path, true);
            }

            DeleteOldTmpDirectories();

            DebugLogger.LogDispose();

            _disposed = true;
        }

        private void DeleteOldTmpDirectories()
        {
            DebugLogger.Log("TemporaryDirectory: ParentFullName: " + Directory.GetParent(Path).FullName);
            DirectoryInfo[] tmpDirs = Directory.GetParent(Path).GetDirectories(_prefix + "*");

            for (int i = 0; i < tmpDirs.Length; i++)
            {
                if (tmpDirs[i].CreationTime < _createdAt)
                {
                    DebugLogger.LogFormat("TemporaryDirectory: Deleting old tmp directory[{0}/{1}]: {2}", (i + 1), tmpDirs.Length, tmpDirs[i].FullName);
                    DirectoryOperations.Delete(tmpDirs[i].FullName, true);
                }
            }
        }
    }
}