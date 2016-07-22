using System;

namespace PatchKit.Unity.Patcher
{
    [Serializable]
    public struct PatcherConfiguration
    {
        /// <summary>
        /// Application secret.
        /// </summary>
        public string AppSecret;

        /// <summary>
        /// Location of application downloaded by patcher.
        /// </summary>
        public string ApplicationDataPath;

        /// <summary>
        /// Forced application version to download. If equals to <c>0</c>, the newest application version is downloaded.
        /// </summary>
        public int ForceVersion;
    }
}