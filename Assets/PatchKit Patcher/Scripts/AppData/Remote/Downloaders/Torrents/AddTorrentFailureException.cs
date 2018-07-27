using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents
{
    [Serializable]
    public class AddTorrentFailureException : TorrentClientException
    {
        public AddTorrentFailureException() : base()
        {
        }

        public AddTorrentFailureException(string message) : base(message)
        {
        }

        public AddTorrentFailureException(string message, Exception inner) : base(message, inner)
        {
        }

        protected AddTorrentFailureException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}