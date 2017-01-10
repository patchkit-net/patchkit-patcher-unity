namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    internal class VersionIntegrity
    {
        public FileIntegrity[] Files { get; private set; }

        public VersionIntegrity(FileIntegrity[] files)
        {
            Files = files;
        }
    }
}