using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    [Serializable]
    public class MissingLocalDataFileException : Exception
    {
        public MissingLocalDataFileException()
        {
        }

        public MissingLocalDataFileException(string message) : base(message)
        {
        }

        public MissingLocalDataFileException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MissingLocalDataFileException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}