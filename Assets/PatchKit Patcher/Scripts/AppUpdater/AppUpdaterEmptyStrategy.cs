using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdaterEmptyStrategy : IAppUpdaterStrategy
    {
        public void Update(CancellationToken cancellationToken)
        {
        }
    }
}