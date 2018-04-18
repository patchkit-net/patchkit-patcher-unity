using System;
using System.Net;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    [Serializable]
    public class IncompleteDataException : Exception
    {
        public IncompleteDataException()
        {
        }

        public IncompleteDataException(string message) : base(message)
        {
        }

        public IncompleteDataException(string message, Exception inner) : base(message, inner)
        {
        }

        protected IncompleteDataException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}