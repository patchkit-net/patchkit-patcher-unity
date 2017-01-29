using System.IO;
using Castle.Core.Internal;
using UnityEditor;
using UnityEngine;

namespace PatchKit.Unity.Editor
{
    public static class CleanTemporaryAppDirectories
    {
        [MenuItem("Edit/Clean Temporary App Directories")]
        public static void Clean()
        {
            string tempPath = Application.dataPath.Replace("/Assets", "/Temp");

            Directory.GetDirectories(tempPath, "PatcherApp*", SearchOption.TopDirectoryOnly).ForEach(s =>
            {
                Directory.Delete(s, true);
            });
        }
    }
}
