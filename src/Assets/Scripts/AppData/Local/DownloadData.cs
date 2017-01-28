using System;
using System.Collections.Generic;
using System.IO;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class DownloadData : IDownloadData
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(DownloadData));

        /// <summary>
        /// Keeps currently used paths. 
        /// Prevents from creating two instances that points to the same directory.
        /// </summary>
        private static readonly List<string> CurrentInstances = new List<string>();

        private readonly string _path;

        private bool _disposed;

        private bool _hasWriteAccess;

        public DownloadData(string path)
        {
            AssertChecks.IsFalse(CurrentInstances.Contains(path),
                "You cannot create two instances of DownloadData pointing to the same path.");
            Checks.ArgumentNotNullOrEmpty(path, "path");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(path, "path");

            _path = path;

            CurrentInstances.Add(_path);
        }

        public void EnableWriteAccess()
        {
            DebugLogger.Log("Enabling write access.");

            if (!_hasWriteAccess)
            {
                DebugLogger.Log(string.Format("Creating root directory {0}", _path));

                Directory.CreateDirectory(_path);
                _hasWriteAccess = true;
            }
        }

        private void CheckWriteAccess()
        {
            AssertChecks.IsTrue(_hasWriteAccess, "Write access is required for this operation (it chould be obtained with EnableWriteAccess method).");
        }

        public string GetFilePath(string fileName)
        {
            CheckWriteAccess();
            Checks.ArgumentNotNullOrEmpty(fileName, "fileName");
            return Path.Combine(_path, fileName);
        }

        public string GetContentPackagePath(int versionId)
        {
            CheckWriteAccess();
            Checks.ArgumentValidVersionId(versionId, "versionId");
            return GetFilePath("-content-" + versionId);
        }

        public string GetDiffPackagePath(int versionId)
        {
            CheckWriteAccess();
            Checks.ArgumentValidVersionId(versionId, "versionId");
            return GetFilePath("-diff-" + versionId);
        }

        public void Clear()
        {
            CheckWriteAccess();
            DebugLogger.Log("Clearing download data.");

            if (Directory.Exists(_path))
            {
                Directory.Delete(_path, true);
                Directory.CreateDirectory(_path);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DownloadData()
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

            CurrentInstances.Remove(_path);

            _disposed = true;
        }
    }
}