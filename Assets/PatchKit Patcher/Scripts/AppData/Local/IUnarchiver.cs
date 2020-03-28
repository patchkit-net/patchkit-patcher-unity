using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Local
{
    public interface IUnarchiver
    {
        event UnarchiveProgressChangedHandler UnarchiveProgressChanged;

        void Unarchive(CancellationToken cancellationToken);

        // set to true to continue unpacking on error. Check HasErrors later to see if there are any
        bool ContinueOnError { set; }

        // After Unarchive() if set to true, there were unpacking errors.
        bool HasErrors { get; }
    }
}