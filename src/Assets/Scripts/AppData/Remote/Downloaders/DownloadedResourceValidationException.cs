using System;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    public class DownloadedResourceValidationException : Exception
    {
        public DownloadedResourceValidationException(string message) : base(message)
        {
        }
    }
}