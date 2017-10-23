using System;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public interface IAppUpdaterStrategyResolver
    {
        StrategyType Resolve(AppUpdaterContext context);

        IAppUpdaterStrategy Create(StrategyType type, AppUpdaterContext context);

        StrategyType GetFallbackStrategy(StrategyType strategyType);
    }
}