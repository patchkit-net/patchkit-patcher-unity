using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEngine.SceneManagement;

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

        [MenuItem("Tools/Build/OSX x64")]
        public static void BuildOsx64 ()
        {
            Build(BuildTarget.StandaloneOSXIntel64);
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
                    return "Patcher";

                case BuildTarget.StandaloneOSXIntel64:
                    return "Patcher.app";
                default:
                    throw new ArgumentOutOfRangeException("target", target, null);
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

            Patching.Unity.Patcher patcher = null;
            foreach (var scenePath in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                SceneManager.SetActiveScene(scene);

                patcher = Patching.Unity.Patcher.Instance;

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

            if (patcher.EditorAppSecret != Patching.Unity.Patcher.EditorAllowedSecret)
            {
                if (EditorUtility.DisplayDialog("Error", "Please reset the editor app secret to continue building.", "Reset the secret and continue", "Cancel"))
                {
                    patcher.EditorAppSecret = Patching.Unity.Patcher.EditorAllowedSecret;

                    var activeScene = SceneManager.GetActiveScene();

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