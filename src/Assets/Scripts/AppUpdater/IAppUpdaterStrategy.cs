using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public interface IAppUpdaterStrategy
    {
        void Patch(CancellationToken cancellationToken);
    }
}