using PatchKit.Unity.Patcher;
using PatchKit.Unity.Patcher.Cancellation;

public abstract class IErrorDialog
{
    public abstract void Display(PatcherError error, CancellationToken cancellationToken);
}
