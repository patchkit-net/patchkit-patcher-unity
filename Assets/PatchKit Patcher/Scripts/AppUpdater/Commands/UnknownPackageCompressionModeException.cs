using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    [Serializable]
    public class UnknownPackageCompressionModeException : Exception
    {
        public UnknownPackageCompressionModeException()
        {
        }

        public UnknownPackageCompressionModeException(string message) : base(message)
        {
        }

        public UnknownPackageCompressionModeException(string message, Exception inner) : base(message, inner)
        {
        }

        protected UnknownPackageCompressionModeException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}