using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PatchKit.Unity.Editor
{
    public class PatcherVersionInfoCreator
    {
        public static void SaveVersionInfo()
        {
            try
            {
                var versionInfo = GetVersionInfo();
                Debug.Log("Writing version info: " + versionInfo);
                File.WriteAllText(Path.Combine(Application.streamingAssetsPath, "patcher.versioninfo"), versionInfo);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("Unable to save patcher version info.");
            }
        }

        private static string GetVersionInfo()
        {
            return Patcher.Version.Value;
        }
    }
}
