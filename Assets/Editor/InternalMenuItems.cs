using UnityEditor;
using UnityEngine;

namespace PatchKit.Unity.Editor
{

public class InternalMenuItems
{
    [MenuItem("Tools/PatchKit Patcher Internal/Upload Asset Store Package", false, 1)]
    public static void UploadAssetStorePackage()
    {
        if (string.IsNullOrEmpty(InternalSettings.Email) || string.IsNullOrEmpty(InternalSettings.Password))
        {
            Debug.LogError("Please set email and password in internal settings");
            return;
        }

        AssetStoreBatchMode.UploadAssetStorePackage(
            InternalSettings.Email, InternalSettings.Password,
            "PatchKit Patcher",
            new [] {"PatchKit Patcher", "Plugins/PatchKit", "Plugins/UniRx", "StreamingAssets"});
    }

    [MenuItem("Tools/PatchKit Patcher Internal/Settings", false, 100)]
    public static void Settings()
    {
        InternalSettings.Show();
    }
}

} // namespace
