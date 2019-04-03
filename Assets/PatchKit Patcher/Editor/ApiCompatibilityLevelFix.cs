using UnityEditor;

namespace PatchKit.Unity
{
[InitializeOnLoad]
public class ApiCompatibilityLevelFix
{
    static ApiCompatibilityLevelFix()
    {
        EditorApplication.delayCall += Fix;
    }

    public static void Fix()
    {
#if UNITY_2017_1_OR_NEWER
        if (PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Standalone) != ApiCompatibilityLevel.NET_4_6)
        {
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_4_6);
        }
#endif
    }
}
}