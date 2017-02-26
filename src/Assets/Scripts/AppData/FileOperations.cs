using System;
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

            try
            {
                DebugLogger.Log(string.Format("Copying file from <{0}> to <{1}> {2}...",
                sourceFilePath,
                destinationFilePath,
                overwrite ? "(overwriting)" : string.Empty));

                File.Copy(sourceFilePath, destinationFilePath, overwrite);

                DebugLogger.Log("File copied.");
            }
            catch (Exception)
            {
                DebugLogger.Log("Error while copying file: an exception occured. Rethrowing exception.");
                throw;
            }
        }

        public static void Delete(string filePath)
        {
            Checks.ArgumentNotNullOrEmpty(filePath, "filePath");
            Checks.FileExists(filePath);

            try
            {
                DebugLogger.Log(string.Format("Deleting file <{0}>.", filePath));

                File.Delete(filePath);

                DebugLogger.Log("File deleted.");
            }
            catch (Exception)
            {
                DebugLogger.Log("Error while deleting file: an exception occured. Rethrowing exception.");
                throw;
            }
        }

        public static void Move(string sourceFilePath, string destinationFilePath)
        {
            Checks.ArgumentNotNullOrEmpty(sourceFilePath, "sourceFilePath");
            Checks.ArgumentNotNullOrEmpty(destinationFilePath, "destinationFilePath");
            Checks.FileExists(sourceFilePath);
            Checks.ParentDirectoryExists(destinationFilePath);

            try
            {
                DebugLogger.Log(string.Format("Moving file from <{0}> to <{1}>.", sourceFilePath, destinationFilePath));

                File.Move(sourceFilePath, destinationFilePath);

                DebugLogger.Log("File moved.");
            }
            catch (Exception)
            {
                DebugLogger.Log("Error while moving file: an exception occured. Rethrowing exception.");
                throw;
            }
        }
    }
}