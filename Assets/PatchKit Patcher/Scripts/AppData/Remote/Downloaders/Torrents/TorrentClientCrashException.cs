using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents
{
    public class TorrentClientCrashException : Exception
    {
        public TorrentClientCrashException()
        {
        }

        public TorrentClientCrashException(string message) : base(message)
        {
        }

        public TorrentClientCrashException(string message, Exception inner) : base(message, inner)
        {
        }

        protected TorrentClientCrashException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}