namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public interface ICheckVersionIntegrityCommand : IAppUpdaterCommand
    {
        VersionIntegrity Results { get; }
    }
}