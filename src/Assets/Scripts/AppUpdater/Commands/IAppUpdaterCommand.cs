using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public interface IAppUpdaterCommand
    {
        void Execute(CancellationToken cancellationToken);

        void Prepare(IStatusMonitor statusMonitor);
    }
}