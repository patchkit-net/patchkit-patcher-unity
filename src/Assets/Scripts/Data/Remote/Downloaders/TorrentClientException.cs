using System;

namespace PatchKit.Unity.Patcher.Data.Remote.Downloaders
{
    public class TorrentClientException : Exception
    {
        public TorrentClientException(string message) : base(message)
        {
        }
    }
}