namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public interface IInstallContentCommand : IAppUpdaterCommand
    {
        void SetPackagePath(string packagePath);
    }
}