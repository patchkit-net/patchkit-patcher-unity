using System;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public interface IAppUpdaterStrategyResolver
    {
        StrategyType Resolve(AppUpdaterContext context, CancellationToken cancellationToken);

        IAppUpdaterStrategy Create(StrategyType type, AppUpdaterContext context);

        StrategyType GetFallbackStrategy(StrategyType strategyType);
    }
}