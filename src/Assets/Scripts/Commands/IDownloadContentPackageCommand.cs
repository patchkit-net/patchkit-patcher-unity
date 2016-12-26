namespace PatchKit.Unity.Patcher.Commands
{
    internal interface IDownloadContentPackageCommand : ICommand
    {
        string PackagePath { get; }
    }
}