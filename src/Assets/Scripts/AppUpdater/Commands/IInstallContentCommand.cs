namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    internal interface IInstallContentCommand : IAppUpdaterCommand
    {
        void SetPackagePath(string packagePath);
    }
}