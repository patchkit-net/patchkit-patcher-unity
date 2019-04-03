using UnityEditor;
using UnityEditor.SceneManagement;

namespace PatchKit.Unity
{
[InitializeOnLoad]
public static class ScriptingRuntimeVersionFix
{
    static ScriptingRuntimeVersionFix()
    {
        EditorApplication.delayCall += Fix;
    }

    public static void Fix()
    {
#if UNITY_2017_1_OR_NEWER
        if (PlayerSettings.scriptingRuntimeVersion != ScriptingRuntimeVersion.Latest)
        {
            EditorUtility.DisplayDialog("Required change of scripting runtime",
                "PatchKit Patcher doesn't support .NET 3.5 scripting runtime. " +
                "It needs to be changed to .NET 4.x. " +
                "The action will be performed automatically, after clicking the OK button." +
                "\n\n" +
                "Unity Editor will be closed and you will need to open the project again." +
                "\n\n" +
                "All current changes will be saved.",
                "OK");

            PlayerSettings.scriptingRuntimeVersion = ScriptingRuntimeVersion.Latest;

            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            AssetDatabase.SaveAssets();

            EditorApplication.Exit(0);
        }
#endif
    }
}
}