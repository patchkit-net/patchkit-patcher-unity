using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Diagnostics;

namespace PatchKit.Unity
{
    public class CustomBuildScripts 
    {
        [MenuItem("Tools/Build/Windows x86")]
        public static void BuildWindows86 ()
        {
            Build(BuildTarget.StandaloneWindows);
        }

        [MenuItem("Tools/Build/Windows x64")]
        public static void BuildWindows64 ()
        {
            Build(BuildTarget.StandaloneWindows64);
        }

        [MenuItem("Tools/Build/Linux x86")]
        public static void BuildLinux86 ()
        {
            Build(BuildTarget.StandaloneLinux);
        }

        [MenuItem("Tools/Build/Linux x64")]
        public static void BuildLinux64 ()
        {
            Build(BuildTarget.StandaloneLinux64);
        }

        [MenuItem("Tools/Build/Linux Universal")]
        public static void BuildLinux ()
        {
            Build(BuildTarget.StandaloneLinuxUniversal);
        }

        [MenuItem("Tools/Build/OSX")]
        public static void BuildOsx ()
        {
            Build(BuildTarget.StandaloneOSXIntel);
        }

        [MenuItem("Tools/Build/OSX x64")]
        public static void BuildOsx64 ()
        {
            Build(BuildTarget.StandaloneOSXIntel64);
        }

        [MenuItem("Tools/Build/OSX Universal")]
        public static void BuildOsxUniversal ()
        {
            Build(BuildTarget.StandaloneOSXUniversal);
        }

        private static string PatcherExecutableName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Patcher.exe";

                case BuildTarget.StandaloneLinux:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneLinuxUniversal:
                    return "Patcher";

                case BuildTarget.StandaloneOSXIntel:
                case BuildTarget.StandaloneOSXIntel64:
                case BuildTarget.StandaloneOSXUniversal:
                    return "Patcher.app";

                default:
                    return "";
            }
        }

        private static void Build(BuildTarget target)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string[] scenePaths = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenePaths.Count() == 0)
            {
                EditorUtility.DisplayDialog("Error", "Add or enable scenes to be included in the Build Settings menu.", "Ok");
                return;
            }

            Patcher.Patcher patcher = null;
            foreach (var scenePath in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                EditorSceneManager.SetActiveScene(scene);

                patcher = PatchKit.Unity.Patcher.Patcher.Instance;

                if (patcher)
                {
                    break;
                }
            }

            if (!patcher)
            {
                EditorUtility.DisplayDialog("Error", "Couldn't resolve an instance of the Patcher component in any of the build scenes.", "Ok");
                return;
            }

            if (patcher.EditorAppSecret != PatchKit.Unity.Patcher.Patcher.EditorAllowedSecret)
            {
                if (EditorUtility.DisplayDialog("Error", "Please reset the editor app secret to continue building.", "Reset the secret and continue", "Cancel"))
                {
                    patcher.EditorAppSecret = PatchKit.Unity.Patcher.Patcher.EditorAllowedSecret;

                    var activeScene = EditorSceneManager.GetActiveScene();

                    EditorSceneManager.MarkSceneDirty(activeScene);
                    EditorSceneManager.SaveScene(activeScene);
                }
                else
                {
                    return;
                }
            }

            BuildOptions buildOptions = BuildOptions.ForceEnableAssertions 
                                    | BuildOptions.ShowBuiltPlayer;

            string path = EditorUtility.SaveFolderPanel("Choose where to build the Patcher", "", "");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string error = BuildPipeline.BuildPlayer(scenePaths, path + "/" + PatcherExecutableName(target), target, buildOptions);

            if (!string.IsNullOrEmpty(error))
            {
                EditorUtility.DisplayDialog("Error", error, "Ok");
            }
        }
    }
}