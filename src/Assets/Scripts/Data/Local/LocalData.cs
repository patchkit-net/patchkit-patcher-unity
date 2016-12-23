using System;
using System.IO;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.Data.Local
{
    internal class LocalData : ILocalData
    {
        private const string MetaDataFileName = "patcher_cache.json";

        private readonly DebugLogger _debugLogger;

        public readonly string Path;

        public LocalData(string path)
        {
            _debugLogger = new DebugLogger(this);

            _debugLogger.Log("Initialization");
            _debugLogger.LogTrace("path = " + path);

            Path = path;
            MetaData = new LocalMetaData(System.IO.Path.Combine(Path, MetaDataFileName));
        }

        public ILocalMetaData MetaData { get; private set; }

        public virtual void CreateDirectory(string dirName)
        {
            _debugLogger.Log(string.Format("Creating directory {0}", dirName));

            string dirPath = GetEntryPath(dirName);

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        public virtual void DeleteDirectory(string dirName)
        {
            _debugLogger.Log(string.Format("Deleting directory {0}", dirName));

            string dirPath = GetEntryPath(dirName);

            if (Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath);
            }
        }

        public virtual bool DirectoryExists(string dirName)
        {
            string dirPath = GetEntryPath(dirName);

            return Directory.Exists(dirPath);
        }

        public virtual bool IsDirectoryEmpty(string dirName)
        {
            string dirPath = GetEntryPath(dirName);

            if (!Directory.Exists(dirName))
            {
                throw new ArgumentException(string.Format("Directory doesn't exist {0}", dirPath), "dirName");
            }

            return Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories).Length == 0;
        }

        public virtual void CreateOrUpdateFile(string fileName, string sourceFilePath)
        {
            _debugLogger.Log(string.Format("Copying file {0} from {1}", fileName, sourceFilePath));

            if (!File.Exists(sourceFilePath))
            {
                throw new ArgumentException(string.Format("Source file doesn't exist {0}", sourceFilePath), "sourceFilePath");
            }

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
            _debugLogger.Log(string.Format("Deleting file {0}", fileName));

            string filePath = GetEntryPath(fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public virtual bool FileExists(string fileName)
        {
            string filePath = GetEntryPath(fileName);

            return File.Exists(filePath);
        }

        public virtual string GetFilePath(string fileName)
        {
            return GetEntryPath(fileName);
        }

        private string GetEntryPath(string entryName)
        {
            return System.IO.Path.Combine(Path, entryName);
        }
    }
}
