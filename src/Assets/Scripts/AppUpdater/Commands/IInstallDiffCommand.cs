namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    internal interface IInstallDiffCommand : IAppUpdaterCommand
    {
        void SetPackagePath(string packagePath);
    }
}