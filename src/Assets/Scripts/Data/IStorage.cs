namespace PatchKit.Unity.Patcher.Data
{
    internal interface IStorage
    {
        /// <summary>
        /// Creates the directory if it doesn't already exist.
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
        /// Creates the file. 
        /// If the file already exists it is overwritten.
        /// If file directory doesn't exist it is created.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="sourceFilePath">The source file path.</param>
        void CreateFile(string fileName, string sourceFilePath);

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
        /// Gets the file path.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        string GetFilePath(string fileName);
    }
}
