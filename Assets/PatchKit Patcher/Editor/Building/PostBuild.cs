using System.CodeDom;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Callbacks;

namespace PatchKit.Unity
{
    public class PostBuild
    {
        private static readonly BuildTarget[] InvalidBuildTargets = { BuildTarget.StandaloneOSXUniversal };

        [PostProcessBuild, UsedImplicitly]
        private static void Execute(BuildTarget buildTarget, string buildPath)
        {
            if (InvalidBuildTargets.Contains(buildTarget))
            {
                string archString = buildTarget.ToString();
                EditorUtility.DisplayDialog("Warning", string.Format("PatchKit Patcher doesn't officialy support {0} architecture. Error may occur.", archString), "Ok");
            }
        }
    }
}