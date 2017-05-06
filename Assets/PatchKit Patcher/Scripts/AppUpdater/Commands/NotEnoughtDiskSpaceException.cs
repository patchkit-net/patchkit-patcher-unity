namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class NotEnoughtDiskSpaceException : InstallerException
    {
        public NotEnoughtDiskSpaceException(string message) : base(message)
        {
        }
    }
}