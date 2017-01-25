using System;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public interface ITemporaryData : IDisposable
    {
        /// <summary>
        /// Enables the write access in temporary data.
        /// Write access is required for operations that are modyfing the data:
        /// <see cref="GetUniquePath"/>
        /// </summary>
        void EnableWriteAccess();

        /// <summary>
        /// Returns the unique path located in temporary data.
        /// </summary>
        string GetUniquePath();
    }
}