using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    internal interface IAppUpdaterCommand
    {
        void Execute(CancellationToken cancellationToken);

        void Prepare(IStatusMonitor statusMonitor);
    }
}