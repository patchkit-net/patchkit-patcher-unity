using System.IO;
using JetBrains.Annotations;
using PatchKit.Unity.Patcher.UI;
using UnityEditor;
using UnityEditor.Callbacks;

namespace PatchKit.Unity.Editor
{
    public static class ScreenSizeCorrection
    {
        [PostProcessBuild, UsedImplicitly]
        public static void SaveScreenSize(BuildTarget buildTarget, string buildPath)
        {
            string content = string.Format("{0} {1}", PlayerSettings.defaultScreenWidth, PlayerSettings.defaultScreenHeight);
            string filename = Path.Combine(CustomBuilding.PatcherDataDirectory(buildTarget, buildPath), BorderlessWindow.ScreenSizeFilename);

            File.WriteAllText(filename, content);
        }
    }
}
