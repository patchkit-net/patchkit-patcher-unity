using System.IO;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    internal class DownloadData : IDownloadData
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(DownloadData));

        private readonly string _path;

        public DownloadData(string path)
        {
            DebugLogger.LogConstructor();
            DebugLogger.LogVariable(path, "path");

            Checks.ArgumentNotNullOrEmpty(path, "path");

            _path = path;

            if (!Directory.Exists(_path))
            {
                Directory.CreateDirectory(_path);
            }
        }

        public string GetFilePath(string fileName)
        {
            Checks.ArgumentNotNullOrEmpty(fileName, "fileName");
            return Path.Combine(_path, fileName);
        }

        public string GetContentPackagePath(int versionId)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            return GetFilePath("-content-" + versionId);
        }

        public string GetDiffPackagePath(int versionId)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            return GetFilePath("-diff-" + versionId);
        }

        public void Clear()
        {
            DebugLogger.Log("Clearing download data.");

            if (Directory.Exists(_path))
            {
                Directory.Delete(_path, true);
                Directory.CreateDirectory(_path);
            }
        }
    }
}