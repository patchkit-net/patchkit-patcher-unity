namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public interface IInstallContentCommand : IAppUpdaterCommand
    {
    	// If set to true, patcher should execute content repair after this operation.
        // This may be needed if there were recoverable errors.
        bool NeedRepair { get; }
    }
}