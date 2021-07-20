namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class FilePathTooLongException : InstallerException
    {
        public FilePathTooLongException(string message) : base(message)
        {
        }
    }
}
