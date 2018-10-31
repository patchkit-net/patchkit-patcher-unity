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

        public FileIntegrityStatus Status { get; set; }

        public static FileIntegrity WithMessage<T>(T expectedValue, T actualValue, FileIntegrityStatus status, string filePath)
        {
            string message = string.Format("Expected {0}, but is {1}", expectedValue, actualValue);
            return new FileIntegrity(filePath, status, message);
        }

        public static FileIntegrity InvalidVersion(int expectedVersion, int actualVersion, string filePath)
        {
            return WithMessage(expectedVersion, actualVersion, FileIntegrityStatus.InvalidVersion, filePath);
        }

        public static FileIntegrity InvalidSize(long expectedSize, long actualSize, string filePath)
        {
            return WithMessage(expectedSize, actualSize, FileIntegrityStatus.InvalidSize, filePath);
        }

        public static FileIntegrity InvalidHash(string expectedHash, string actualHash, string filePath)
        {
            return WithMessage(expectedHash, actualHash, FileIntegrityStatus.InvalidHash, filePath);
        }
    }
}