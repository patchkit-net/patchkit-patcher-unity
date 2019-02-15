using UnityEditor;
using UnityEngine;

namespace PatchKit.Unity.Editor
{

public class InternalMenuItems
{
    [MenuItem("Tools/PatchKit Patcher Internal/Settings", false, 100)]
    public static void Settings()
    {
        InternalSettings.ShowSettings();
    }
}

} // namespace
