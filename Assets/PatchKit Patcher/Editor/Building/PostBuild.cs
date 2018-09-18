using System.CodeDom;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Callbacks;

namespace PatchKit.Unity
{
    public class PostBuild
    {
        private static readonly BuildTarget[] ValidBuildTargets = {
            BuildTarget.StandaloneLinux,
            BuildTarget.StandaloneLinux64,
            BuildTarget.StandaloneWindows,
            BuildTarget.StandaloneWindows64,
            BuildTarget.StandaloneOSXIntel64,
        };

        [PostProcessBuild, UsedImplicitly]
        private static void Execute(BuildTarget buildTarget, string buildPath)
        {
            if (!ValidBuildTargets.Contains(buildTarget))
            {
                string archString = buildTarget.ToString();
                EditorUtility.DisplayDialog("Warning", string.Format("PatchKit Patcher doesn't officially support {0} architecture. Errors may occur.", archString), "Ok");
            }
        }
    }
}