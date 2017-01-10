using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    internal interface IAppUpdaterStrategy
    {
        void Patch(CancellationToken cancellationToken);
    }
}