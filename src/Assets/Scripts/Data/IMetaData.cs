namespace PatchKit.Unity.Patcher.Data
{
    internal interface IMetaData
    {
        /// <summary>
        /// Returns the names of files present in the data.
        /// </summary>
        string[] GetFileNames();

        /// <summary>
        /// Adds or updates the file in the data.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="versionId">The version identifier.</param>
        void AddOrUpdateFile(string fileName, int versionId);

        /// <summary>
        /// Removes the file from the data if it exists.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        void RemoveFile(string fileName);

        /// <summary>
        /// Determines whether file exists in the data.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        bool FileExists(string fileName);

        /// <summary>
        /// Returns file version in data. Note that file must exist.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        int GetFileVersion(string fileName);
    }
}
