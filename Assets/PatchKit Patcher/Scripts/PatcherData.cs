using System;

namespace PatchKit.Unity.Patcher
{
    [Serializable]
    public struct PatcherData
    {
        public string AppSecret;

        public string AppDataPath;

        public string LockFilePath;

        public int OverrideLatestVersionId;

        public bool? IsOnline;
    }
}