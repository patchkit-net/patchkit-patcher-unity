using System;
using System.IO;
using PatchKit.Unity.Utilities;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Debug
{
    public static class PatcherInfo
    {
        public static string GetVersion()
        {
            string patcherVersion = "(unknown)";

            UnityDispatcher.Invoke(() =>
            {
                try
                {
                    string versionFilePath = Path.Combine(Application.streamingAssetsPath, "patcher.versioninfo");

                    if (File.Exists(versionFilePath))
                    {
                        patcherVersion = File.ReadAllText(versionFilePath);
                    }
                }
                catch (Exception ex)
                {
                    patcherVersion = string.Format("(unable to load version because: {0})", ex);
                }
            }).WaitOne();

            return patcherVersion;
        }
    }
}