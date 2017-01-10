namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    internal interface IDownloadDiffPackageCommand : IAppUpdaterCommand
    {
        string PackagePath { get; }

        void SetKeySecret(string keySecret);
    }
}