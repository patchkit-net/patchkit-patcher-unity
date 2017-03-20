namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// Meta information about local applciation data.
    /// </summary>
    public interface ILocalMetaData
    {
        /// <summary>
        /// Returns names of all registered entries.
        /// </summary>
        string[] GetRegisteredEntries();

        /// <summary>
        /// Registers the entry with specified version id.
        /// If entry is already registered then it is overwritten.
        /// </summary>
        /// <param name="entryName">Name of the entry.</param>
        /// <param name="versionId">The version id.</param>
        void RegisterEntry(string entryName, int versionId);

        /// <summary>
        /// Unregisters the entry.
        /// </summary>
        /// <param name="entryName">Name of the entry.</param>
        void UnregisterEntry(string entryName);

        /// <summary>
        /// Determines whether entry exists.
        /// </summary>
        /// <param name="entryName">Name of the entry.</param>
        bool IsEntryRegistered(string entryName);

        /// <summary>
        /// Returns the version id of the entry.
        /// </summary>
        /// <param name="entryName">Name of the entry.</param>
        int GetEntryVersionId(string entryName);
    }
}