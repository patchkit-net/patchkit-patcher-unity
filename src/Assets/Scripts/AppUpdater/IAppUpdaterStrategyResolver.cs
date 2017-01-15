namespace PatchKit.Unity.Patcher.AppUpdater
{
    public interface IAppUpdaterStrategyResolver
    {
        IAppUpdaterStrategy Resolve(AppUpdaterContext context);
    }
}