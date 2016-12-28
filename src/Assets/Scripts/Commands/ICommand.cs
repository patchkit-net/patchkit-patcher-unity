using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Progress;

namespace PatchKit.Unity.Patcher.Commands
{
    internal interface ICommand
    {
        void Execute(CancellationToken cancellationToken);

        void Prepare(IProgressMonitor progressMonitor);
    }
}