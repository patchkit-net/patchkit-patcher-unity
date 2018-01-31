using System;
using System.Runtime.Serialization;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    [Serializable]
    public class LocalMetaDataEntryNotFoundException : Exception
    {
        public LocalMetaDataEntryNotFoundException()
        {
        }

        public LocalMetaDataEntryNotFoundException(string message) : base(message)
        {
        }

        public LocalMetaDataEntryNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }

        protected LocalMetaDataEntryNotFoundException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}