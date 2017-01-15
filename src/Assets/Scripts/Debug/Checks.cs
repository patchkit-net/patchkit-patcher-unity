using System;
using System.IO;
using PatchKit.Unity.Patcher.AppData.Remote;

namespace PatchKit.Unity.Patcher.Debug
{
    public class Checks : BaseChecks
    {
        private static ValidationFailedHandler ArgumentValidationFailed(string name)
        {
            return message =>
            {
                throw new ArgumentException(string.Format("Argument \"{0}\": {1}", name, message), name);
            };
        }

        public static void ArgumentValidVersionId(int versionId, string name)
        {
            ValidVersionId(versionId, ArgumentValidationFailed(name));
        }

        public static void ArgumentValidRemoteResource(RemoteResource resource, string name)
        {
            ValidRemoteResource(resource, ArgumentValidationFailed(name));
        }

        public static void ArgumentMoreThanZero<T>(T value, string name) where T : IComparable
        {
            MoreThanZero(value, ArgumentValidationFailed(name));
        }

        public static void ArgumentNotNull(object value, string name)
        {
            NotNull(value, ArgumentValidationFailed(name));
        }

        public static void ArgumentNotNullOrEmpty(string value, string name)
        {
            NotNullOrEmpty(value, ArgumentValidationFailed(name));
        }

        public static void FileExists(string filePath)
        {
            FileExists(filePath, message => { throw new FileNotFoundException(message, filePath); });
        }

        public static void ArgumentFileExists(string filePath, string name)
        {
            FileExists(filePath, ArgumentValidationFailed(name));
        }

        public static void ParentDirectoryExists(string path)
        {
            ParentDirectoryExists(path, message => { throw new DirectoryNotFoundException(message); });
        }

        public static void ArgumentParentDirectoryExists(string path, string name)
        {
            ParentDirectoryExists(path, ArgumentValidationFailed(name));
        }

        public static void DirectoryExists(string dirPath)
        {
            DirectoryExists(dirPath, message => { throw new DirectoryNotFoundException(message); });
        }

        public static void ArgumentDirectoryExists(string dirPath, string name)
        {
            DirectoryExists(dirPath, ArgumentValidationFailed(name));
        }
    }
}