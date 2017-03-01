using System;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class DownloaderException : Exception
    {
        public readonly DownloaderExceptionStatus Status;

        public DownloaderException(string message, DownloaderExceptionStatus status) : base(message)
        {
            Status = status;
        }
    }
}