using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.Commands
{
    internal interface ICommand
    {
        void Execute(CancellationToken cancellationToken);

        void Prepare(IStatusMonitor statusMonitor);
    }
}