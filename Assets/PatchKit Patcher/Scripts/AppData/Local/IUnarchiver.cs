using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public interface IUnarchiver
    {
        event UnarchiveProgressChangedHandler UnarchiveProgressChanged;

        void Unarchive(CancellationToken cancellationToken);
    }
}