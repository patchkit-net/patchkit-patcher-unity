using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    [Serializable]
    public class DownloadFailureException : Exception
    {
        public DownloadFailureException()
        {
        }

        public DownloadFailureException(string message) : base(message)
        {
        }

        public DownloadFailureException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DownloadFailureException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}