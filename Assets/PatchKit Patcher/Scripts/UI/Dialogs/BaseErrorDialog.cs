using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public abstract class BaseErrorDialog : Dialog<BaseErrorDialog>
    {
        public abstract void Display(PatcherError patcherError, CancellationToken cancellationToken);
    }
}