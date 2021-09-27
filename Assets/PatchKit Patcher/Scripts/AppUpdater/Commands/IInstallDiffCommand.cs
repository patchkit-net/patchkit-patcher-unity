using PatchKit.Unity.Patcher.AppData.Local;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public interface IInstallDiffCommand : IAppUpdaterCommand
    {
        string[] GetBrokenFiles();
    }
}