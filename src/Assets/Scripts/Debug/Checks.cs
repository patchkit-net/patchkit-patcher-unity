using System;
using System.IO;
using PatchKit.Unity.Patcher.Data.Remote;

namespace PatchKit.Unity.Patcher.Debug
{
    internal static class Checks
    {
        private static void Argument(bool condition, string name, string message)
        {
            if (!condition)
            {
                throw new ArgumentException(message, name);
            }
        }

        public static void ArgumentValidVersionId(int versionId, string name)
        {
            Argument(versionId > 0, name, "Invalid version id.");
        }

        public static void ArgumentValidRemoteResource(RemoteResource resource, string name)
        {
            Argument(resource.Size > 0, name, "Resource size is not more than zero.");
            Argument(!string.IsNullOrEmpty(resource.HashCode), name, "Resource hash code is null or empty.");
            Argument(resource.Urls != null && resource.Urls.Length > 0, name, "Resource urls are null or empty.");
            Argument(resource.TorrentUrls != null && resource.TorrentUrls.Length > 0, name, "Resource torrent urls are null or empty.");
        }

        public static void ArgumentMoreThanZero(int value, string name)
        {
            Argument(value > 0, name, "Argument is not more than zero.");
        }

        public static void ArgumentNotNullOrEmpty(string value, string name)
        {
            Argument(!string.IsNullOrEmpty(value), name, "Argument is null or empty.");
        }

        public static void ArgumentFileExists(string filePath, string name)
        {
            Argument(File.Exists(filePath), name, "File doesn't exists.");
        }

        public static void ArgumentDirectoryOfFileExists(string filePath, string name)
        {
            string dirPath = Path.GetDirectoryName(filePath);

            Argument(dirPath == null || Directory.Exists(dirPath), name, "Directory of file doesn't exists.");
        }

        public static void ArgumentDirectoryExists(string dirPath, string name)
        {
            Argument(Directory.Exists(dirPath), name, "Directory doesn't exists.");
        }
    }
}