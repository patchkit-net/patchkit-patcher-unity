using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    [Serializable]
    public class InvalidChunkDataException : Exception
    {
        public InvalidChunkDataException()
        {
        }

        public InvalidChunkDataException(string message) : base(message)
        {
        }

        public InvalidChunkDataException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InvalidChunkDataException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}