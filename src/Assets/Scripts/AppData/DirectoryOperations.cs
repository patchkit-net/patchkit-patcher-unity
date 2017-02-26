using System;
using System.IO;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData
{
    // ReSharper disable once InconsistentNaming
    public static class DirectoryOperations
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(DirectoryOperations));

        public static bool IsDirectoryEmpty(string dirPath)
        {
            Checks.ArgumentNotNullOrEmpty(dirPath, "dirPath");
            Checks.DirectoryExists(dirPath);

            return Directory.GetFiles(dirPath, "*", SearchOption.TopDirectoryOnly).Length == 0 &&
                   Directory.GetDirectories(dirPath, "*", SearchOption.TopDirectoryOnly).Length == 0;
        }

        public static void CreateParentDirectory(string path)
        {
            Checks.ArgumentNotNullOrEmpty(path, "path");

            try
            {
                DebugLogger.Log(string.Format("Creating parent directory for <{0}>.", path));

                string dirPath = Path.GetDirectoryName(path);

                if (!string.IsNullOrEmpty(dirPath))
                {
                    CreateDirectory(dirPath);
                }

                DebugLogger.Log("Parent directory created.");
            }
            catch (Exception)
            {
                DebugLogger.Log("Error while creating parent directory: an exception occured. Rethrowing exception.");
                throw;
            }
        }

        public static void CreateDirectory(string dirPath)
        {
            Checks.ArgumentNotNullOrEmpty(dirPath, "dirPath");

            try
            {
                DebugLogger.Log(string.Format("Creating directory <{0}>.", dirPath));

                Directory.CreateDirectory(dirPath);

                DebugLogger.Log("Directory created.");
            }
            catch (Exception)
            {
                DebugLogger.Log("Error while creating directory: an exception occured. Rethrowing exception.");
                throw;
            }
        }

        public static void Delete(string dirPath, bool recursive)
        {
            Checks.ArgumentNotNullOrEmpty(dirPath, "dirPath");
            Checks.DirectoryExists(dirPath);

            try
            {
                DebugLogger.Log(string.Format("Deleting directory {0}<{1}>.",
                    recursive ? "recursively " : string.Empty,
                    dirPath));

                Directory.Delete(dirPath, recursive);

                DebugLogger.Log("Directory deleted.");
            }
            catch (Exception)
            {
                DebugLogger.Log("Error while deleting directory: an exception occured. Rethrowing exception.");
                throw;
            }
        }
    }
}