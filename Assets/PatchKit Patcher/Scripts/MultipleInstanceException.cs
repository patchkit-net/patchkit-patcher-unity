using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher
{
    [Serializable]
    internal class MultipleInstanceException: Exception
    {
        public MultipleInstanceException()
        {
        }

        public MultipleInstanceException(string message) : base(message)
        {
        }

        public MultipleInstanceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MultipleInstanceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}