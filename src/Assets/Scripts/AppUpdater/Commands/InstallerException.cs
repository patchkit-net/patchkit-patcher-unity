using System;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    internal class InstallerException : Exception
    {
        public InstallerException(string message) : base(message)
        {
        }
    }
}