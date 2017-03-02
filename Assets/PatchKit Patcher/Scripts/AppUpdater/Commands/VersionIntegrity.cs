namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public class VersionIntegrity
    {
        public FileIntegrity[] Files { get; private set; }

        public VersionIntegrity(FileIntegrity[] files)
        {
            Files = files;
        }
    }
}