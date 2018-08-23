using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    [Serializable]
    public class InvalidLocalDataFileVersionException : Exception
    {
        public InvalidLocalDataFileVersionException()
        {
        }

        public InvalidLocalDataFileVersionException(string message) : base(message)
        {
        }

        public InvalidLocalDataFileVersionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InvalidLocalDataFileVersionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}