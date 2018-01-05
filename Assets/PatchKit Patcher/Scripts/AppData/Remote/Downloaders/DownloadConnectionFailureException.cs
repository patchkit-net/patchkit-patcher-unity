using System;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class DownloadConnectionFailureException : Exception
    {
        public DownloadConnectionFailureException(string url) : base(string.Format("Connection to download server has failed for {0}", url))
        {
        }
    }
}