using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public enum StrategyType
    {
        None,
        Empty,
        Content,
        Diff,
        Repair,
        RepairAndDiff
    }

    public interface IAppUpdaterStrategy
    {
        // if set to true, attempt to repair by using AppRepairer on error.
        // Makes sense to set it to false from ongoing repair process.
        // default: true
        bool RepairOnError { get; set; }

        void Update(CancellationToken cancellationToken);

        StrategyType GetStrategyType();
    }
}