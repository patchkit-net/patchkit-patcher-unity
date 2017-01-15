namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public interface IInstallDiffCommand : IAppUpdaterCommand
    {
        void SetPackagePath(string packagePath);
    }
}