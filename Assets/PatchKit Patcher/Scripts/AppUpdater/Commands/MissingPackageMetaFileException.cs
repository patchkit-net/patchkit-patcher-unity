using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    [Serializable]
    public class MissingPackageMetaFileException : Exception
    {
        public MissingPackageMetaFileException()
        {
        }

        public MissingPackageMetaFileException(string message) : base(message)
        {
        }

        public MissingPackageMetaFileException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MissingPackageMetaFileException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}