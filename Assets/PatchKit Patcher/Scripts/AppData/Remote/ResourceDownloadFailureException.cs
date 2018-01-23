using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    [Serializable]
    public class ResourceDownloadFailureException : Exception
    {
        public ResourceDownloadFailureException()
        {
        }

        public ResourceDownloadFailureException(string message) : base(message)
        {
        }

        public ResourceDownloadFailureException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ResourceDownloadFailureException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}