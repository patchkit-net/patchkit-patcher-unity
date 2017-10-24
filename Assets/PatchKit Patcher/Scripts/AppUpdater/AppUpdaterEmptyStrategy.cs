using System;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterEmptyStrategy: IAppUpdaterStrategy
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdaterStrategyResolver));

        public StrategyType GetStrategyType()
        {
            return StrategyType.Empty;
        }

        public void Update(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Updating with empty strategy. Doing nothing. ");
        }
    }
}