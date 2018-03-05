namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class FileIntegrity
    {
        public FileIntegrity(string fileName, FileIntegrityStatus status, string message = null)
        {
            FileName = fileName;
            Status = status;
            Message = message;
        }

        public string FileName { get; private set; }

        public readonly string Message;

        public FileIntegrityStatus Status { get; private set; }
    }
}