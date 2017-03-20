namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// Writable directory.
    /// Must be prepared (with <see cref="PrepareForWriting"/>) before using for write purposes.
    /// </summary>
    public interface IWritableDirectory
    {
        /// <summary>
        /// The directory path.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Prepares the directory for writing operations.
        /// </summary>
        /// <exception cref="System.UnauthorizedAccessException">Thrown when there are problems with write access.</exception>
        void PrepareForWriting();
    }
}