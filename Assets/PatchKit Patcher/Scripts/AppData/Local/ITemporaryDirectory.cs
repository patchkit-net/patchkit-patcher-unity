using System;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    /// <summary>
    /// Temporary directory. 
    /// Deleted after destroying or disposing the object.
    /// </summary>
    /// <seealso cref="IWritableDirectory" />
    /// <seealso cref="IDisposable" />
    public interface ITemporaryDirectory : IWritableDirectory, IDisposable
    {
        /// <summary>
        /// Returns the unique path located in temporary directory.
        /// </summary>
        string GetUniquePath();
    }
}