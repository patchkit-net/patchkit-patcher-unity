using System;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class InstallerException : Exception
    {
        public InstallerException(string message) : base(message)
        {
        }
    }
}