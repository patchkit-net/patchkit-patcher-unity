using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    [Serializable]
    public class MissingFileFromPackageException : Exception
    {
        public MissingFileFromPackageException()
        {
        }

        public MissingFileFromPackageException(string message) : base(message)
        {
        }

        public MissingFileFromPackageException(string message, Exception inner) : base(message, inner)
        {
        }

        protected MissingFileFromPackageException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}