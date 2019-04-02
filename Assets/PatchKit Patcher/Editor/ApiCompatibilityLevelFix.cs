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
#if UNITY_5_6_OR_NEWER
        if (PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Standalone) != ApiCompatibilityLevel.NET_2_0)
        {
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_2_0);
        }
#else
        if (PlayerSettings.apiCompatibilityLevel != ApiCompatibilityLevel.NET_2_0)
        {
            PlayerSettings.apiCompatibilityLevel = ApiCompatibilityLevel.NET_2_0;
        }
#endif
    }
}
}