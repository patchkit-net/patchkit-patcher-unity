namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    internal interface IValidateLicenseCommand : IAppUpdaterCommand
    {
        string KeySecret { get; }
    }
}