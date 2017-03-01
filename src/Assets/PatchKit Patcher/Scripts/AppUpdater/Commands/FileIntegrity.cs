namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class FileIntegrity
    {
        public FileIntegrity(string fileName, FileIntegrityStatus status)
        {
            FileName = fileName;
            Status = status;
        }

        public string FileName { get; private set; }

        public FileIntegrityStatus Status { get; private set; }
    }
}