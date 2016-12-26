using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher
{
    internal interface IPatcherStrategy
    {
        void Patch(CancellationToken cancellationToken);
    }
}