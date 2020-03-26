using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterEmptyStrategy: IAppUpdaterStrategy
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdaterStrategyResolver));

        // not used
        public bool RepairOnError { get; set; }

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