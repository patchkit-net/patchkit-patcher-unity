using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public abstract class BaseAppUpdaterCommand : IAppUpdaterCommand
    {
        private bool _executeHasBeenCalled;
        private bool _prepareHasBeenCalled;

        public bool NeedRepair { get; protected set; }

        public virtual void Execute(CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _executeHasBeenCalled, "Execute");
            Assert.IsTrue(_prepareHasBeenCalled, "Command not prepared.");
        }

        public virtual void Prepare(UpdaterStatus status, CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _prepareHasBeenCalled, "Prepare");
        }
    }
}