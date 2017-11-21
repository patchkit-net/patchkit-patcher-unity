using System;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    [Serializable]
    public class AppUpdaterConfiguration
    {
        public bool UseTorrents;

        public bool CheckConsistencyBeforeDiffUpdate;

        public long HashSizeThreshold = 1024 * 1024 * 1024; // in bytes
    }
}