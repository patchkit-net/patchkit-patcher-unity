using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppUpdater.Commands
{
    public interface IAppUpdaterCommand
    {
        void Execute(CancellationToken cancellationToken);

        void Prepare(UpdaterStatus status, CancellationToken cancellationToken);
    }
}