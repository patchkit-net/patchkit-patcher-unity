using System;

namespace PatchKit.Unity.Patcher.Commands
{
    internal class InstallerException : Exception
    {
        public InstallerException(string message) : base(message)
        {
        }
    }
}