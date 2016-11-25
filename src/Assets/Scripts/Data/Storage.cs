using System;
using System.IO;
using PatchKit.Unity.Patcher.Log;

namespace PatchKit.Unity.Patcher.Data
{
    internal class Storage : IDebugLogger
    {
        public readonly string Path;

        public Storage(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Gets the entry path.
        /// </summary>
        /// <param name="entryName">Name of the entry.</param>
        private string GetEntryPath(string entryName)
        {
            return System.IO.Path.Combine(Path, entryName);
        }

        /// <summary>
        /// Creates the directory if it doesn't already exist.
        /// </summary>
        /// <param name="dirName">Name of the directory.</param>
        public virtual void CreateDirectory(string dirName)
        {
            this.Log(string.Format("Creating directory <{0}>", dirName));

            string dirPath = GetEntryPath(dirName);

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }

        /// <summary>
        /// Deletes the directory if it exists.
        /// </summary>
        /// <param name="dirName">Name of the directory.</param>
        public virtual void DeleteDirectory(string dirName)
        {
            this.Log(string.Format("Trying to delete directory <{0}>", dirName));

            string dirPath = GetEntryPath(dirName);

            if (Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath);
            }
        }

        /// <summary>
        /// Determines whether directory exists.
        /// </summary>
        /// <param name="dirName">Name of the directory.</param>
        public virtual bool DirectoryExists(string dirName)
        {
            string dirPath = GetEntryPath(dirName);

            return Directory.Exists(dirPath);
        }

        /// <summary>
        /// Determines whether directory is empty.
        /// </summary>
        /// <param name="dirName">Name of the directory.</param>
        public virtual bool IsDirectoryEmpty(string dirName)
        {
            string dirPath = GetEntryPath(dirName);

            if (!Directory.Exists(dirName))
            {
                throw new ArgumentException(string.Format("Directory doesn't exist <{0}>", dirPath), "dirName");
            }

            return Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories).Length == 0;
        }

        /// <summary>
        /// Creates the file. 
        /// If the file already exists it is overwritten.
        /// If file directory doesn't exist it is created.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="sourceFilePath">The source file path.</param>
        public virtual void CreateFile(string fileName, string sourceFilePath)
        {
            this.Log(string.Format("Copying file <{0}> from <{1}>", fileName, sourceFilePath));

            if (!File.Exists(sourceFilePath))
            {
                throw new ArgumentException(string.Format("Source file doesn't exist <{0}>", sourceFilePath), "sourceFilePath");
            }

            string filePath = GetEntryPath(fileName);

            string fileDirectoryPath = System.IO.Path.GetDirectoryName(filePath);

            if (fileDirectoryPath != null && !Directory.Exists(fileDirectoryPath))
            {
                Directory.CreateDirectory(fileDirectoryPath);
            }

            File.Copy(sourceFilePath, filePath, true);
        }

        /// <summary>
        /// Deletes the file if it exists.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public virtual void DeleteFile(string fileName)
        {
            this.Log(string.Format("Deleting file <{0}>", fileName));

            string filePath = GetEntryPath(fileName);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Determines whether file exists.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public virtual bool FileExists(string fileName)
        {
            string filePath = GetEntryPath(fileName);

            return File.Exists(filePath);
        }

        /// <summary>
        /// Gets the file path.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public virtual string GetFilePath(string fileName)
        {
            return GetEntryPath(fileName);
        }
    }
}
