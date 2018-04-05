using System;
using PatchKit.Apps.Updating.AppUpdater;

namespace PatchKit.Patching.Unity
{
    [Serializable]
    public struct PatcherConfiguration
    {
        public AppUpdaterConfiguration AppUpdaterConfiguration;

        public bool AutomaticallyStartApp;

        public bool AutomaticallyCheckForAppUpdates;

        public bool AutomaticallyInstallApp;
    }
}