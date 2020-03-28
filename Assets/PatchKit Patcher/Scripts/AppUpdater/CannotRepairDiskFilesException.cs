namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class CannotRepairDiskFilesException : System.Exception
    {
        public CannotRepairDiskFilesException(string message) : base(message)
        {
        }
    }
}