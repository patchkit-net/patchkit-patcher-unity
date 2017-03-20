namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public interface IValidateLicenseCommand : IAppUpdaterCommand
    {
        string KeySecret { get; }
    }
}