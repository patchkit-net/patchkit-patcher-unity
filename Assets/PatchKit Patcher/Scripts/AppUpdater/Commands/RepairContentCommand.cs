using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public interface IRepairContentCommand : IAppUpdaterCommand
    {    
    }

    public class RepairContentCommand : IRepairContentCommand
    {
        public void Execute(CancellationToken cancellationToken)
        {
        }

        public void Prepare(IStatusMonitor statusMonitor)
        {
        }
    }
}