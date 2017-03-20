using System;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    [Serializable]
    public struct AppUpdaterConfiguration
    {
        public bool UseTorrents;

        public bool CheckConsistencyBeforeDiffUpdate;
    }
}