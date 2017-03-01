using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.Status;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public abstract class BaseAppUpdaterCommand : IAppUpdaterCommand
    {
        private bool _executeHasBeenCalled;
        private bool _prepareHasBeenCalled;

        public virtual void Execute(CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _executeHasBeenCalled, "Execute");
            Assert.IsTrue(_prepareHasBeenCalled, "Command not prepared.");
        }

        public virtual void Prepare(IStatusMonitor statusMonitor)
        {
            Assert.MethodCalledOnlyOnce(ref _prepareHasBeenCalled, "Prepare");
        }
    }
}