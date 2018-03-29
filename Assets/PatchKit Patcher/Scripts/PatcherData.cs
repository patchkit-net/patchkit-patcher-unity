using System;

namespace PatchKit.Patching.Unity
{
    [Serializable]
    public struct PatcherData
    {
        public string AppSecret;

        public string AppDataPath;

        public string LockFilePath;

        public int OverrideLatestVersionId;
    }
}