using System;

namespace PatchKit.Unity.Patcher.AppUpdater
{
[Serializable]
public struct PatcherConfiguration
{
    public bool AutomaticallyStartApp;

    public bool AutomaticallyCheckForAppUpdates;

    public bool AutomaticallyInstallApp;
}
}