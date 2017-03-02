using System.IO;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData
{
    // ReSharper disable once InconsistentNaming
    public static class FileOperations
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(FileOperations));

        public static void Copy(string sourceFilePath, string destinationFilePath, bool overwrite)
        {
            Checks.ArgumentNotNullOrEmpty(sourceFilePath, "sourceFilePath");
            Checks.ArgumentNotNullOrEmpty(destinationFilePath, "destinationFilePath");
            Checks.FileExists(sourceFilePath);
            Checks.ParentDirectoryExists(destinationFilePath);

            DebugLogger.Log(string.Format("Copying file from <{0}> to <{1}> {2}.", 
                sourceFilePath, 
                destinationFilePath, 
                overwrite ? "(overwriting)" : string.Empty));

            File.Copy(sourceFilePath, destinationFilePath, overwrite);
        }

        public static void Delete(string filePath)
        {
            Checks.ArgumentNotNullOrEmpty(filePath, "filePath");
            Checks.FileExists(filePath);

            DebugLogger.Log(string.Format("Deleting file <{0}>.", filePath));

            File.Delete(filePath);
        }

        public static void Move(string sourceFilePath, string destinationFilePath)
        {
            Checks.ArgumentNotNullOrEmpty(sourceFilePath, "sourceFilePath");
            Checks.ArgumentNotNullOrEmpty(destinationFilePath, "destinationFilePath");
            Checks.FileExists(sourceFilePath);
            Checks.ParentDirectoryExists(destinationFilePath);

            DebugLogger.Log(string.Format("Moving file from <{0}> to <{1}>.", sourceFilePath, destinationFilePath));

            File.Move(sourceFilePath, destinationFilePath);
        }
    }
}