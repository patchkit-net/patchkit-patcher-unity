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
        /// Location of application that will be downloaded by patcher.
        /// In editor this path is always set to Temp/Patcher directory in project files.
        /// </summary>
        public string ApplicationDataPath;

        /// <summary>
        /// Forced application version to download. If equals to <c>0</c>, the newest application version is downloaded.
        /// Works only in editor/development build. 
        /// </summary>
        public int ForceVersion;
    }
}