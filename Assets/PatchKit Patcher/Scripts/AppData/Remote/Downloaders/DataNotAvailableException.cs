using System;
using System.Net;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    [Serializable]
    public class DataNotAvailableException : Exception
    {
        public DataNotAvailableException()
        {
        }

        public DataNotAvailableException(string message) : base(message)
        {
        }

        public DataNotAvailableException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DataNotAvailableException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}