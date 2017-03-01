using System;
using System.IO;
using PatchKit.Unity.Patcher.AppData.Remote;

namespace PatchKit.Unity.Patcher.Debug
{
    public class BaseChecks
    {
        protected delegate void ValidationFailedHandler(string message);

        protected static void ValidVersionId(int versionId, ValidationFailedHandler validationFailed)
        {
            if (versionId < 0)
            {
                validationFailed("Invalid verison id - " + versionId);
            }
        }

        protected static void ValidRemoteResource(RemoteResource resource, ValidationFailedHandler validationFailed)
        {
            if (resource.Size <= 0)
            {
                validationFailed("Resource size is not more than zero - " + resource.Size);
            }
            // TODO: Sometimes it is...
            /*else if (string.IsNullOrEmpty(resource.HashCode))
            {
                validationFailed("Resource hash code is null or empty.");
            }*/ 
            else if (resource.Urls == null || resource.Urls.Length == 0)
            {
                validationFailed("Resource urls are null or empty.");
            }
            else if (resource.TorrentUrls == null || resource.TorrentUrls.Length == 0)
            {
                validationFailed("Resource torrent urls are null or empty.");
            }
        }

        protected static void MoreThanZero<T>(T value, ValidationFailedHandler validationFailed) where T : IComparable
        {
            if (value.CompareTo(Convert.ChangeType(0, typeof(T))) <= 0)
            {
                validationFailed("Value is not more than zero.");
            }
        }

        protected static void NotNull(object value, ValidationFailedHandler validationFailed)
        {
            if (value == null)
            {
                validationFailed("Value is null.");
            }
        }

        protected static void NotNullOrEmpty(string value, ValidationFailedHandler validationFailed)
        {
            if (string.IsNullOrEmpty(value))
            {
                validationFailed("Value is null or empty.");
            }
        }

        protected static void FileExists(string filePath, ValidationFailedHandler validationFailed)
        {
            if (!File.Exists(filePath))
            {
                validationFailed("File doesn't exists - " + filePath);
            }
        }

        protected static void ParentDirectoryExists(string path, ValidationFailedHandler validationFailed)
        {
            if (path == null)
            {
                validationFailed("Cannot find parent directory of null path.");
            }

            string dirPath = Path.GetDirectoryName(path);

            if (dirPath == null)
            {
                validationFailed("Cannot find parent directory of root path.");
            }
            else if (!Directory.Exists(dirPath))
            {
                validationFailed("Parent directory doesn't exist - " + path);
            }
        }

        protected static void DirectoryExists(string dirPath, ValidationFailedHandler validationFailed)
        {
            if (dirPath == null)
            {
                validationFailed("Directory doesn't exists - null");
            }
            else if (!Directory.Exists(dirPath))
            {
                validationFailed("Directory doesn't exists - " + dirPath);
            }
        }
    }
}