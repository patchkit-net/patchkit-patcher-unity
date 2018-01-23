using System;

namespace PatchKit.Unity.Patcher
{
    public class NonLauncherExecutionException : Exception
    {
        public NonLauncherExecutionException() : base("Patcher has been started without a Launcher.")
        {
        }
    }
}