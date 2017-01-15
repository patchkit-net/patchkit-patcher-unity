using System;
using System.Collections.Generic;
using System.IO;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public class LocalData : ILocalData
    {
        private static readonly List<string> CurrentInstances = new List<string>();

        private const string MetaDataFileName = "patcher_cache.json";

        private const string TemporaryDataDirectoryName = "temp";

        private const string DownloadDataDirectoryName = "downloads";

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(LocalData));

        private ITemporaryData _temporaryData;

        private IDownloadData _downloadData;

        private bool _writeAccess;

        public readonly string Path;

        public LocalData(string path)
        {
            AssertChecks.IsFalse(CurrentInstances.Contains(path),
                "You cannot create two instances of LocalData pointing to the same path.");
            Checks.ArgumentNotNullOrEmpty(path, "path");

            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(path, "path");

            Path = path;
            MetaData = new LocalMetaData(System.IO.Path.Combine(Path, MetaDataFileName));
            
            CurrentInstances.Add(Path);
        }

        public ILocalMetaData MetaData { get; private set; }

        public ITemporaryData TemporaryData
        {
            get
            {
                AssertChecks.IsTrue(_writeAccess, "Cannot use TemporaryData without write access.");
                return _temporaryData;
            }
        }

        public IDownloadData DownloadData
        {
            get
            {
                AssertChecks.IsTrue(_writeAccess, "Cannot use DownloadData without write access.");
                return _downloadData;
            }
        }

        public void EnableWriteAccess()
        {
            DebugLogger.Log("Enabling write access.");

            if (!_writeAccess)
            {
                if (!Directory.Exists(Path))
                {
                    Directory.CreateDirectory(Path);
                }

                _temporaryData = new TemporaryData(System.IO.Path.Combine(Path, TemporaryDataDirectoryName));
                _downloadData = new DownloadData(System.IO.Path.Combine(Path, DownloadDataDirectoryName));
                _writeAccess = true;
            }
        }

        public virtual void CreateDirectory(string dirName)
        {
            Checks.ArgumentNotNullOrEmpty(dirName, "dirName");

            if (FileExists(dirName))
            {
                throw new InvalidOperationException("File exists - " + dirName);
            }

            DebugLogger.Log(string.Format("Creating directory {0}", dirName));

            string dirPath = GetEntryPath(dirName);

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        public virtual void DeleteDirectory(string dirName)
        {
            Checks.ArgumentNotNullOrEmpty(dirName, "dirName");

            DebugLogger.Log(string.Format("Deleting directory {0}", dirName));

            string dirPath = GetEntryPath(dirName);

            if (Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath);
            }
        }

        public virtual bool DirectoryExists(string dirName)
        {
            Checks.ArgumentNotNullOrEmpty(dirName, "dirName");

            string dirPath = GetEntryPath(dirName);

            return Directory.Exists(dirPath);
        }

        public virtual bool IsDirectoryEmpty(string dirName)
        {
            Checks.ArgumentNotNullOrEmpty(dirName, "dirName");

            string dirPath = GetEntryPath(dirName);

            Checks.DirectoryExists(dirPath);

            return Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories).Length == 0;
        }

        public virtual void CreateOrUpdateFile(string fileName, string sourceFilePath)
        {
            Checks.ArgumentNotNullOrEmpty(fileName, "fileName");
            Checks.ArgumentFileExists(sourceFilePath, "sourceFilePath");

            if (DirectoryExists(fileName))
            {
                throw new InvalidOperationException("Directory exists - " + fileName);
            }

            DebugLogger.Log(string.Format("Copying file {0} from {1}", fileName, sourceFilePath));

            string filePath = GetEntryPath(fileName);

            string fileDirectoryPath = System.IO.Path.GetDirectoryName(filePath);

            if (fileDirectoryPath != null && !Directory.Exists(fileDirectoryPath))
            {
                Directory.CreateDirectory(fileDirectoryPath);
            }

            File.Copy(sourceFilePath, filePath, true);
        }

        public virtual void DeleteFile(string fileName)
        {
            Checks.ArgumentNotNullOrEmpty(fileName, "fileName");

            DebugLogger.Log(string.Format("Deleting file {0}", fileName));

            string filePath = GetEntryPath(fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public virtual bool FileExists(string fileName)
        {
            Checks.ArgumentNotNullOrEmpty(fileName, "fileName");

            string filePath = GetEntryPath(fileName);

            return File.Exists(filePath);
        }

        public virtual string GetFilePath(string fileName)
        {
            Checks.ArgumentNotNullOrEmpty(fileName, "fileName");

            return GetEntryPath(fileName);
        }

        public string GetDirectoryPath(string dirName)
        {
            Checks.ArgumentNotNullOrEmpty(dirName, "dirName");

            return GetEntryPath(dirName);
        }

        private string GetEntryPath(string entryName)
        {
            return System.IO.Path.Combine(Path, entryName);
        }

        public void Dispose()
        {
            CurrentInstances.Remove(Path);
            TemporaryData.Dispose();
        }
    }
}
