using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher
{
    [Serializable]
    public class NonLauncherExecutionException : Exception
    {
        public NonLauncherExecutionException()
        {
        }

        public NonLauncherExecutionException(string message) : base(message)
        {
        }

        public NonLauncherExecutionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected NonLauncherExecutionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}