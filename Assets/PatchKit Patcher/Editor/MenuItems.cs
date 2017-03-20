using UnityEditor;
using UnityEngine;

namespace PatchKit.Unity.Editor
{

public class MenuItems
{
    private const string Root = "Tools/PatchKit Patcher";

    [MenuItem(Root + "/Getting Started...", false, 0)]
    public static void GettingStarted()
    {
        Application.OpenURL("http://docs.patchkit.net/unity_custom_patcher.html");
    }

    [MenuItem(Root + "/Clean All", false, 100)]
    public static void CleanAll()
    {
        CleanTemporaryAppDirectories.Clean();
    }

    [MenuItem(Root + "/Save Version Info", false, 101)]
    public static void SaveVersionInfo()
    {
        PatcherVersionInfoCreator.SaveVersionInfo();
    }

    [MenuItem(Root + "/Home Page...", false, 200)]
    public static void PatchKitHome()
    {
        Application.OpenURL("http://patchkit.net/");
    }

    [MenuItem(Root + "/Publisher Panel...", false, 201)]
    public static void PublisherPanel()
    {
        Application.OpenURL("https://panel.patchkit.net/");
    }

} // class

} // namespace
