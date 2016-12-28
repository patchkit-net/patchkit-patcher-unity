using System;
using System.IO;
using System.Linq;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.Data.Local
{
    internal class LocalData : IDisposable, ILocalData
    {
        private const string MetaDataFileName = "patcher_cache.json";

        private const string TemporaryDataDirectoryName = "temp";

        private const string DownloadDataDirectoryName = "downloads";

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(LocalData));

        public readonly string Path;

        public LocalData(string path)
        {
            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(path, "path");

            Checks.ArgumentNotNullOrEmpty(path, "path");

            Path = path;
            MetaData = new LocalMetaData(System.IO.Path.Combine(Path, MetaDataFileName));
            TemporaryData = new TemporaryData(System.IO.Path.Combine(Path, TemporaryDataDirectoryName));
            DownloadData = new DownloadData(System.IO.Path.Combine(Path, DownloadDataDirectoryName));
        }

        public ILocalMetaData MetaData { get; private set; }

        public ITemporaryData TemporaryData { get; private set; }

        public IDownloadData DownloadData { get; private set; }

        public virtual void CreateDirectory(string dirName)
        {
            DebugLogger.Log(string.Format("Creating directory {0}", dirName));

            Checks.ArgumentNotNullOrEmpty(dirName, "dirName");

            string dirPath = GetEntryPath(dirName);

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        public virtual void DeleteDirectory(string dirName)
        {
            DebugLogger.Log(string.Format("Deleting directory {0}", dirName));

            Checks.ArgumentNotNullOrEmpty(dirName, "dirName");

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

            if (!Directory.Exists(dirName))
            {
                throw new ArgumentException(string.Format("Directory doesn't exist {0}", dirPath), "dirName");
            }

            return Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories).Length == 0;
        }

        public virtual void CreateOrUpdateFile(string fileName, string sourceFilePath)
        {
            DebugLogger.Log(string.Format("Copying file {0} from {1}", fileName, sourceFilePath));

            Checks.ArgumentNotNullOrEmpty(fileName, "fileName");
            Checks.ArgumentFileExists(sourceFilePath, "sourceFilePath");

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
            DebugLogger.Log(string.Format("Deleting file {0}", fileName));

            Checks.ArgumentNotNullOrEmpty(fileName, "fileName");

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

        public bool IsInstalled()
        {
            var fileNames = MetaData.GetFileNames();

            if (fileNames.Length == 0)
            {
                return false;
            }

            int installedVersion = MetaData.GetFileVersion(fileNames[0]);

            return fileNames.All(FileExists) &&
                   fileNames.All(fileName => MetaData.GetFileVersion(fileName) == installedVersion);
        }

        public int GetInstalledVersion()
        {
            if (!IsInstalled())
            {
                throw new InvalidOperationException("Cannot retrieve version when local data is not installed.");
            }

            return MetaData.GetFileVersion(MetaData.GetFileNames()[0]);
        }

        private string GetEntryPath(string entryName)
        {
            return System.IO.Path.Combine(Path, entryName);
        }

        public void Dispose()
        {
            TemporaryData.Dispose();
        }
    }
}
