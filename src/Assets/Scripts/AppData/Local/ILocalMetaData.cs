namespace PatchKit.Unity.Patcher.AppData.Local
{
    public interface ILocalMetaData
    {
        /// <summary>
        /// Returns list of all file names.
        /// </summary>
        string[] GetFileNames();

        /// <summary>
        /// Adds or updates the file with specified version id.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="versionId">The version id.</param>
        void AddOrUpdateFile(string fileName, int versionId);

        /// <summary>
        /// Removes the file if it exists.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        void RemoveFile(string fileName);

        /// <summary>
        /// Determines whether file exists.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        bool FileExists(string fileName);

        /// <summary>
        /// Returns the version id of file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        int GetFileVersionId(string fileName);
    }
}