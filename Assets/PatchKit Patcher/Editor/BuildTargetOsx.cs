using UnityEditor;

namespace PatchKit.Unity
{
    public static class BuildTargetOsx
    {
        public static BuildTarget Get()
        {
#if UNITY_2017_3_OR_NEWER
            return BuildTarget.StandaloneOSX;
#else
            return BuildTarget.StandaloneOSXIntel64;
#endif
        }
    }
}