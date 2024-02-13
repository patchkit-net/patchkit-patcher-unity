﻿using System;
using System.IO;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppData.FileSystem;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;

namespace PatchKit.Unity.Patcher
{
    public class PatcherSenderId
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(PatcherSenderId));

        private static string _senderId;

        public static string Get()
        {
            return _senderId ?? (_senderId = GenerateOrRead());
        }

        private static string GenerateOrRead()
        {
            var filePath = GetFilePath();

            if (File.Exists(Paths.Fix(filePath)))
            {
                string savedSenderId = File.ReadAllText(Paths.Fix(filePath));
                if (!string.IsNullOrEmpty(savedSenderId))
                {
                    DebugLogger.Log("SenderId: " + savedSenderId + " (loaded from " + filePath + ")");

                    return savedSenderId;
                }
            }

            string senderId = Guid.NewGuid().ToString().Replace("-", "");

            string parentDirPath = Path.GetDirectoryName(filePath);
            if (parentDirPath != null)
            {
                DirectoryOperations.CreateDirectory(parentDirPath, CancellationToken.Empty);
            }

            File.WriteAllText(Paths.Fix(filePath), senderId);

            DebugLogger.Log("SenderId: " + senderId + " (saved in " + filePath + ")");

            return senderId;
        }

        private static string GetFilePath()
        {
            const string dirName = "PatchKit";
            const string fileName = "sender_id";

            string dirPath;

            switch (Platform.GetPlatformType())
            {
                case PlatformType.Linux:
                case PlatformType.Windows:
                    var localApplicationDataPath =
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    dirPath = Path.Combine(localApplicationDataPath, dirName);
                    break;
                case PlatformType.OSX:
                    var personalPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    dirPath = Path.Combine(Path.Combine(personalPath, "Application Support"), dirName);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Path.Combine(dirPath, fileName);
        }
    }
}