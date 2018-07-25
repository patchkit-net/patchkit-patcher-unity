using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Callbacks;

namespace PatchKit.Unity.Editor
{
    public static class UnixLaunchScriptCreator
    {
        private const string LaunchScriptContentFile = "Assets/PatchKit Patcher/Editor/patcher.template.sh";
        public const string LaunchScriptName = "run.sh";

        public static string LaunchScriptPath(string buildPath)
        {
            return Path.Combine(Path.GetDirectoryName(buildPath), LaunchScriptName);
        }

        [PostProcessBuild, UsedImplicitly]
        private static void PostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget != BuildTarget.StandaloneLinux64 &&
                buildTarget != BuildTarget.StandaloneLinux &&
                buildTarget != BuildTarget.StandaloneLinuxUniversal)
            {
                return;
            }

            string launchScriptPath = LaunchScriptPath(buildPath);

            File.Copy(LaunchScriptContentFile, launchScriptPath);
        }
    }
}