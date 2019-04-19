using UnityEngine;
using UnityEngine.Assertions;

public partial class Patcher
{
#if UNITY_EDITOR
    public string EditorAppSecret;
    public int EditorOverrideAppLatestVersionId;

    private InitializationData? LoadEditorInitializationData()
    {
        Debug.Log(message: "Loading initialization data from editor.");

        Assert.IsNotNull(value: Application.dataPath);
        Assert.IsNotNull(value: EditorAppSecret);

        return new InitializationData
        {
            AppPath = Application.dataPath.Replace(
                oldValue: "/Assets",
                newValue: $"/Temp/PatcherApp{EditorAppSecret}"),
            AppSecret = EditorAppSecret,
            LockFilePath = null,
            OverrideAppLatestVersionId = EditorOverrideAppLatestVersionId > 0
                ? (int?) EditorOverrideAppLatestVersionId
                : null,
            IsOnline = null
        };
    }
#endif
}