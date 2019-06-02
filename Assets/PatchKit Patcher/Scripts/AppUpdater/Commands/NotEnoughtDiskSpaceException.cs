namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class NotEnoughtDiskSpaceException : InstallerException
    {
        public long AvailableSpace { get; private set; }
        public long RequiredSpace { get; private set; }

        public NotEnoughtDiskSpaceException(string message, long availableSpace, long requiredSpace) : base(message)
        {
            AvailableSpace = availableSpace;
            RequiredSpace = requiredSpace;
        }
    }
}