namespace PatchKit.Unity.Patcher.AppUpdater
{
    internal interface IAppUpdaterStrategyResolver
    {
        IAppUpdaterStrategy Resolve(AppUpdaterContext context);
    }
}