namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public interface IDownloadContentPackageCommand : IAppUpdaterCommand
    {
        string PackagePath { get; }

        void SetKeySecret(string keySecret);
    }
}