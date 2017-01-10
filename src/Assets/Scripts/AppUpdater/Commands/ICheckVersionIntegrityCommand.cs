namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    internal interface ICheckVersionIntegrityCommand : IAppUpdaterCommand
    {
        VersionIntegrity Results { get; }
    }
}