namespace PatchKit.Unity.Patcher.Commands
{
    internal interface IDownloadDiffPackageCommand : ICommand
    {
        string PackagePath { get; }
    }
}