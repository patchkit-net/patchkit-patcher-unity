using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEngine.SceneManagement;

namespace PatchKit.Unity
{
    public class CustomBuildScripts
    {
        [MenuItem("Tools/Build/With build settings/Windows x86")]
        public static void BuildWindows86()
        {
            BuildFromBuildSettings(BuildTarget.StandaloneWindows);
        }

        [MenuItem("Tools/Build/With build settings/Windows x64")]
        public static void BuildWindows64()
        {
            BuildFromBuildSettings(BuildTarget.StandaloneWindows64);
        }

        [MenuItem("Tools/Build/With build settings/Linux x86")]
        public static void BuildLinux86()
        {
            BuildFromBuildSettings(BuildTarget.StandaloneLinux);
        }

        [MenuItem("Tools/Build/With build settings/Linux x64")]
        public static void BuildLinux64()
        {
            BuildFromBuildSettings(BuildTarget.StandaloneLinux64);
        }

        [MenuItem("Tools/Build/With build settings/Linux Universal")]
        public static void BuildLinux()
        {
            BuildFromBuildSettings(BuildTarget.StandaloneLinuxUniversal);
        }

        [MenuItem("Tools/Build/With build settings/OSX")]
        public static void BuildOsx()
        {
            BuildFromBuildSettings(BuildTarget.StandaloneOSXIntel);
        }

        [MenuItem("Tools/Build/With build settings/OSX x64")]
        public static void BuildOsxx64()
        {
            BuildFromBuildSettings(BuildTarget.StandaloneOSXIntel64);
        }

        /////////////////////////////////////////////////////////////////////


        [MenuItem("Tools/Build/Current scene/Windows x86")]
        public static void BuildWindows86WithCurrentScene()
        {
            BuildFromCurrentScene(BuildTarget.StandaloneWindows);
        }

        [MenuItem("Tools/Build/Current scene/Windows x64")]
        public static void BuildWindows64WithCurrentScene()
        {
            BuildFromCurrentScene(BuildTarget.StandaloneWindows64);
        }

        [MenuItem("Tools/Build/Current scene/Linux x86")]
        public static void BuildLinux86WithCurrentScene()
        {
            BuildFromCurrentScene(BuildTarget.StandaloneLinux);
        }

        [MenuItem("Tools/Build/Current scene/Linux x64")]
        public static void BuildLinux64WithCurrentScene()
        {
            BuildFromCurrentScene(BuildTarget.StandaloneLinux64);
        }

        [MenuItem("Tools/Build/Current scene/Linux Universal")]
        public static void BuildLinuxWithCurrentScene()
        {
            BuildFromCurrentScene(BuildTarget.StandaloneLinuxUniversal);
        }

        [MenuItem("Tools/Build/Current scene/OSX")]
        public static void BuildOsxWithCurrentScene()
        {
            BuildFromCurrentScene(BuildTarget.StandaloneOSXIntel);
        }

        [MenuItem("Tools/Build/Current scene/OSX x64")]
        public static void BuildOsx64WithCurrentScene()
        {
            BuildFromCurrentScene(BuildTarget.StandaloneOSXIntel64);
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

        private static void BuildFromBuildSettings(BuildTarget target)
        {
            string[] scenePaths = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            BuildScenes(target, scenePaths);
        }

        private static void BuildFromCurrentScene(BuildTarget target)
        {
            string[] scenePaths = {SceneManager.GetActiveScene().path};

            BuildScenes(target, scenePaths);
        }

        private static void BuildScenes(BuildTarget target, string[] scenePaths)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (!scenePaths.Any())
            {
                EditorUtility.DisplayDialog("Error", "Add or enable scenes to be included in the Build Settings menu.", "Ok");
                return;
            }

            PlayerSettings.defaultScreenWidth = 600;
            PlayerSettings.defaultScreenHeight = 400;
            PlayerSettings.defaultIsFullScreen = false;

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