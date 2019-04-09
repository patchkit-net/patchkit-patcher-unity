using PatchKit.Apps;

namespace PatchKit_Patcher.Scripts
{
    public struct PatcherAppState
    {
        public string Secret { get; }

        public bool IsInstalled { get; }

        public int? InstalledVersion { get; }

        public int? LatestVersion { get; }

        public AppInfo? Info { get; }
    }
}