using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Callbacks;

namespace PatchKit.Patching.Unity.Editor.Building
{
    public class PostBuild
    {
        private static readonly BuildTarget[] InvalidBuildTargets = { BuildTarget.StandaloneOSX };

        [PostProcessBuild, UsedImplicitly]
        private static void Execute(BuildTarget buildTarget, string buildPath)
        {
            if (InvalidBuildTargets.Contains(buildTarget))
            {
                string archString = buildTarget.ToString();
                EditorUtility.DisplayDialog("Warning", string.Format("PatchKit Patcher doesn't officially support {0} architecture. Errors may occur.", archString), "Ok");
            }
        }
    }
}