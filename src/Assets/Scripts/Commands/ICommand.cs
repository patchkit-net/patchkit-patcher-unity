using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.Commands
{
    internal interface ICommand
    {
        void Execute(CancellationToken cancellationToken);
    }
}