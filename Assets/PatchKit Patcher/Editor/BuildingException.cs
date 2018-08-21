using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Editor
{
    public class BuildingException : Exception
    {
        public BuildingException()
        {
        }

        public BuildingException(string message) : base(message)
        {
        }

        public BuildingException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BuildingException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}