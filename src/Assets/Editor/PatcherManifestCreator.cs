using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Callbacks;

namespace PatchKit.Unity.Editor
{
    public static class PatcherManifestCreator
    {
        [PostProcessBuild, UsedImplicitly]
        private static void PostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            string exeFileName = string.Empty;
            string exeArguments = string.Empty;
            
            if (buildTarget == BuildTarget.StandaloneWindows || buildTarget == BuildTarget.StandaloneWindows64)
            {
                exeFileName = string.Format("\\\"{{exedir}}/{0}\\\"", Path.GetFileName(buildPath));
                exeArguments = "--installdir \\\"{installdir}\\\" --secret \\\"{secret}\\\"";  
            }

            if (buildTarget == BuildTarget.StandaloneOSXUniversal ||
                buildTarget == BuildTarget.StandaloneOSXIntel ||
                buildTarget == BuildTarget.StandaloneOSXIntel64)
            {
                exeFileName = "open";
                exeArguments = string.Format("\\\"{{exedir}}/{0}\\\" --args --installdir \\\"{{installdir}}\\\" --secret \\\"{{secret}}\\\"", Path.GetFileName(buildPath));
            }

            if (buildTarget == BuildTarget.StandaloneLinux ||
                buildTarget == BuildTarget.StandaloneLinux64 ||
                buildTarget == BuildTarget.StandaloneLinuxUniversal)
            {
                exeFileName = string.Format("\\\"{{exedir}}/{0}\\\"", Path.GetFileName(buildPath));
                exeArguments = "--installdir \\\"{installdir}\\\" --secret \\\"{secret}\\\"";
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            string manifestPath = Path.Combine(Path.GetDirectoryName(buildPath), "patcher.manifest");

            string manifestContent = "{" + "\n" +
                                        string.Format("    \"exe_fileName\" : \"{0}\"", exeFileName) + "," + "\n" +
                                        string.Format("    \"exe_arguments\" : \"{0}\"", exeArguments) + "\n" +
                                        "}";

            File.WriteAllText(manifestPath, manifestContent);
        }
    }
}