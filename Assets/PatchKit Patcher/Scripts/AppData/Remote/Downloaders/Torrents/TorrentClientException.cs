using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents
{
    public class TorrentClientException : Exception
    {
        public TorrentClientException()
        {
        }

        public TorrentClientException(string message) : base(message)
        {
        }

        public TorrentClientException(string message, Exception inner) : base(message, inner)
        {
        }

        protected TorrentClientException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}