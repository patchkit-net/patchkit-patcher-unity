using System;
using System.Net;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    [Serializable]
    public class ServerErrorException : Exception
    {
        public ServerErrorException()
        {
        }

        public ServerErrorException(string message) : base(message)
        {
        }

        public ServerErrorException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ServerErrorException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}