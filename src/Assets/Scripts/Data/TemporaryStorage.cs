using System;
using System.IO;

namespace PatchKit.Unity.Patcher.Data
{
    internal class TemporaryStorage : Storage, IDisposable
    {
        private bool _disposed;

        public TemporaryStorage(string path) : base(path)
        {
            Directory.CreateDirectory(Path);
        }

        public override void CreateDirectory(string dirName)
        {
            ThrowIfDisposed();
            base.CreateDirectory(dirName);
        }

        public override void DeleteDirectory(string dirName)
        {
            ThrowIfDisposed();
            base.DeleteDirectory(dirName);
        }

        public override bool DirectoryExists(string dirName)
        {
            ThrowIfDisposed();
            return base.DirectoryExists(dirName);
        }

        public override bool IsDirectoryEmpty(string dirName)
        {
            ThrowIfDisposed();
            return base.IsDirectoryEmpty(dirName);
        }

        public override void CreateFile(string fileName, string sourceFilePath)
        {
            ThrowIfDisposed();
            base.CreateFile(fileName, sourceFilePath);
        }

        public override void DeleteFile(string fileName)
        {
            ThrowIfDisposed();
            base.DeleteFile(fileName);
        }

        public override bool FileExists(string fileName)
        {
            ThrowIfDisposed();
            return base.FileExists(fileName);
        }

        public override string GetFilePath(string fileName)
        {
            ThrowIfDisposed();
            return base.GetFilePath(fileName);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null);
            }
        }
    }
}