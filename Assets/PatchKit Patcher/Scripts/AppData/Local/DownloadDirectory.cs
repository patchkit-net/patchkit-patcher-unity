using System.IO;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// Implementation of <see cref="IDownloadDirectory"/>.
    /// </summary>
    /// <seealso cref="BaseWritableDirectory{DownloadDirectory}" />
    /// <seealso cref="IDownloadDirectory" />
    public class DownloadDirectory : BaseWritableDirectory<DownloadDirectory>, IDownloadDirectory
    {
        public DownloadDirectory(string path) : base(path)
        {
        }

        public string GetContentPackagePath(int versionId)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");

            return Path.PathCombine("-content-" + versionId);
        }

        public string GetContentPackageMetaPath(int versionId)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            return Path.PathCombine("-content-meta-" + versionId);
        }

        public string GetDiffPackagePath(int versionId)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");

            return Path.PathCombine("-diff-" + versionId);
        }

        public string GetDiffPackageMetaPath(int versionId)
        {
            Checks.ArgumentValidVersionId(versionId, "versionId");
            return Path.PathCombine("-diff-meta" + versionId);
        }

        public void Clear()
        {
            DebugLogger.Log("Clearing download data.");

            if (Directory.Exists(Path))
            {
                DirectoryOperations.Delete(Path, true);
                DirectoryOperations.CreateDirectory(Path);
            }
        }
    }
}