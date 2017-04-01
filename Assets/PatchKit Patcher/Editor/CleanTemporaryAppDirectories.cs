using System.IO;
using UnityEngine;

namespace PatchKit.Unity.Editor
{
    public static class CleanTemporaryAppDirectories
    {
        public static void Clean()
        {
            string tempPath = Application.dataPath.Replace("/Assets", "/Temp");

            string[] directories = Directory.GetDirectories(tempPath, "PatcherApp*", SearchOption.TopDirectoryOnly);
            foreach (var directory in directories)
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
