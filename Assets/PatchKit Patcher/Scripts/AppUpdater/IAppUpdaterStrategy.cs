using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public enum StrategyType
    {
        None,
        Empty,
        Content,
        Diff
    }

    public interface IAppUpdaterStrategy
    {
        void Update(CancellationToken cancellationToken);

        StrategyType GetStrategyType();
    }
}