using System;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class DownloadDataNotAvailableException : Exception
    {
        public DownloadDataNotAvailableException(string url) : base(string.Format("Download data at {0} is not available.", url))
        {
        }
    }
}