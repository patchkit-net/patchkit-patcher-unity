using System;
using System.IO;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData
{
    // ReSharper disable once InconsistentNaming
    public static class DirectoryOperations
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(DirectoryOperations));

        /// <summary>
        /// Determines whether directory is empty (does not contain any files or directories).
        /// </summary>
        /// <param name="dirPath">The directory path.</param>
        /// <returns>
        ///   <c>true</c> if directory is empty; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="dirPath"/> is null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="dirPath"/> doesn't exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Unauthorized access.</exception>
        public static bool IsDirectoryEmpty(string dirPath)
        {
            Checks.ArgumentNotNullOrEmpty(dirPath, "dirPath");
            Checks.DirectoryExists(dirPath);

            try
            {
                DebugLogger.Log(string.Format("Checking whether directory is empty <{0}>...", dirPath));

                bool isEmpty = Directory.GetFiles(dirPath, "*", SearchOption.TopDirectoryOnly).Length == 0 &&
                    Directory.GetDirectories(dirPath, "*", SearchOption.TopDirectoryOnly).Length == 0;

                DebugLogger.Log(string.Format("Check complete: directory is {0}.", isEmpty ? "empty" : "not empty"));

                return isEmpty;
            }
            catch (Exception)
            {
                DebugLogger.LogError("Error while checking whether directory is empty: an exception occured. Rethrowing exception.");
                throw;
            }
            
        }

        /// <summary>
        /// Creates parent directory for specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> is null or empty.</exception>
        /// <exception cref="UnauthorizedAccessException">Unauthorized access.</exception>
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
                DebugLogger.LogError("Error while creating parent directory: an exception occured. Rethrowing exception.");
                throw;
            }
        }

        /// <summary>
        /// Creates the directory.
        /// </summary>
        /// <param name="dirPath">The directory path.</param>
        /// <exception cref="ArgumentException"><paramref name="dirPath"/> is null or empty.</exception>
        /// <exception cref="UnauthorizedAccessException">Unauthorized access.</exception>
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
                DebugLogger.LogError("Error while creating directory: an exception occured. Rethrowing exception.");
                throw;
            }
        }

        /// <summary>
        /// Creates the directory.
        /// </summary>
        /// <param name="dirPath">The directory path.</param>
        /// <param name="recursive">if set to <c>true</c> then directory content is also removed recursively.</param>
        /// <exception cref="ArgumentException"><paramref name="dirPath" /> is null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="dirPath" /> doesn't exist.</exception>
        /// <exception cref="UnauthorizedAccessException">Unauthorized access.</exception>
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
                DebugLogger.LogError("Error while deleting directory: an exception occured. Rethrowing exception.");
                throw;
            }
        }
    }
}