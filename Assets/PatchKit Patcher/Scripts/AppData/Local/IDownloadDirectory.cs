namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// Directory for storing all of the download data.
    /// </summary>
    /// <seealso cref="IWritableDirectory" />
    public interface IDownloadDirectory : IWritableDirectory
    {
        /// <summary>
        /// Returns path to the version content package located in download directory.
        /// </summary>
        /// <param name="versionId">The version id.</param>
        string GetContentPackagePath(int versionId);

        /// <summary>
        /// Return path to the version content package meta file.
        /// </summary>
        /// <param name="versionId"></param>
        /// <returns></returns>
        string GetContentPackageMetaPath(int versionId);

        /// <summary>
        /// Returns path to the version diff package located in download directory.
        /// </summary>
        /// <param name="versionId">The version id.</param>
        string GetDiffPackagePath(int versionId);

        /// <summary>
        /// Return path to the version diff package meta file.
        /// </summary>
        /// <param name="versionId"></param>
        /// <returns></returns>
        string GetDiffPackageMetaPath(int versionId);

        /// <summary>
        /// Clears the download data.
        /// </summary>
        void Clear();
    }
}