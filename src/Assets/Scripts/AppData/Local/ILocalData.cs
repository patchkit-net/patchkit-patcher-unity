using System;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public interface ILocalData : IDisposable
    {
        /// <summary>
        /// Enables the write access in local data.
        /// Write access is required for operations that are modyfing the data:
        /// <see cref="CreateDirectory"/>, <see cref="DeleteDirectory"/>, <see cref="CreateOrUpdateFile"/>, <see cref="DeleteFile"/>
        /// </summary>
        void EnableWriteAccess();

        /// <summary>
        /// Creates the directory unless it exists.
        /// </summary>
        /// <param name="dirName">Name of the directory.</param>
        void CreateDirectory(string dirName);

        /// <summary>
        /// Deletes the directory if it exists.
        /// </summary>
        /// <param name="dirName">Name of the directory.</param>
        void DeleteDirectory(string dirName);

        /// <summary>
        /// Determines whether directory exists.
        /// </summary>
        /// <param name="dirName">Name of the directory.</param>
        bool DirectoryExists(string dirName);

        /// <summary>
        /// Determines whether directory is empty.
        /// </summary>
        /// <param name="dirName">Name of the directory.</param>
        bool IsDirectoryEmpty(string dirName);

        /// <summary>
        /// Creates or updates the file with content from source file path.
        /// If parent directory doesn't exist it is created.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="sourceFilePath">The source file path.</param>
        void CreateOrUpdateFile(string fileName, string sourceFilePath);

        /// <summary>
        /// Deletes the file if it exists.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        void DeleteFile(string fileName);

        /// <summary>
        /// Determines whether file exists.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        bool FileExists(string fileName);

        /// <summary>
        /// Returns the file path.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        string GetFilePath(string fileName);

        /// <summary>
        /// Gets the directory path.
        /// </summary>
        /// <param name="dirName">Name of the directory.</param>
        string GetDirectoryPath(string dirName);
    }
}