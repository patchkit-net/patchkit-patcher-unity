using System;

namespace PatchKit.Unity.Patcher.Data.Remote.Downloaders
{
    public class DownloaderException : Exception
    {
        public DownloaderException(string message) : base(message)
        {
        }
    }
}