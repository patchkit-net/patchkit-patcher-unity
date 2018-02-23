using System;
using System.Runtime.Serialization;

namespace PatchKit.Patching.Unity
{
    [Serializable]
    internal class MultipleInstancesException: Exception
    {
        public MultipleInstancesException()
        {
        }

        public MultipleInstancesException(string message) : base(message)
        {
        }

        public MultipleInstancesException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MultipleInstancesException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}