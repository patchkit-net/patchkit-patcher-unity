namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public interface IDownloadDiffPackageCommand : IAppUpdaterCommand
    {
        string PackagePath { get; }

        void SetKeySecret(string keySecret);
    }
}