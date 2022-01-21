using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.UI.Dialogs
{
    public abstract class AErrorDialog : Dialog<AErrorDialog>
    {
        public abstract void Display(PatcherError patcherError, CancellationToken cancellationToken);
    }
}