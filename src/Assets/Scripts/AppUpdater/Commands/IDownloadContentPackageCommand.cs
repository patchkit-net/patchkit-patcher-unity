namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    internal interface IDownloadContentPackageCommand : IAppUpdaterCommand
    {
        string PackagePath { get; }

        void SetKeySecret(string keySecret);
    }
}