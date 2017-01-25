using System;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public interface IDownloadData : IDisposable
    {
        /// <summary>
        /// Enables the write access in download data.
        /// Write access is required for operations that are modyfing the data:
        /// <see cref="GetFilePath"/>, <see cref="GetContentPackagePath"/>, <see cref="GetDiffPackagePath"/>, <see cref="Clear"/>
        /// </summary>
        void EnableWriteAccess();

        /// <summary>
        /// Returns the file path located in download data.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        string GetFilePath(string fileName);

        /// <summary>
        /// Returns the certain version content package path located in download data.
        /// </summary>
        /// <param name="versionId">The version id.</param>
        string GetContentPackagePath(int versionId);

        /// <summary>
        /// Returns the certain version diff package path located in download data.
        /// </summary>
        /// <param name="versionId">The version id.</param>
        string GetDiffPackagePath(int versionId);

        /// <summary>
        /// Clears the download data.
        /// </summary>
        void Clear();
    }
}