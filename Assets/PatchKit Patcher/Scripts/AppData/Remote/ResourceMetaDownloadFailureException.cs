using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppData.Remote
{
    [Serializable]
    public class ResourceMetaDownloadFailureException : Exception
    {
        public ResourceMetaDownloadFailureException()
        {
        }

        public ResourceMetaDownloadFailureException(string message) : base(message)
        {
        }

        public ResourceMetaDownloadFailureException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ResourceMetaDownloadFailureException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}