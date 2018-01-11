using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    [Serializable]
    public class ConnectionFailureException : Exception
    {
        public ConnectionFailureException()
        {
        }

        public ConnectionFailureException(string message) : base(message)
        {
        }

        public ConnectionFailureException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ConnectionFailureException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}