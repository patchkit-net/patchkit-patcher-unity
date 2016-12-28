using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Progress;

namespace PatchKit.Unity.Patcher.Commands
{
    internal class ValidateLicenseCommand : IValidateLicenseCommand
    {
        private readonly PatcherContext _context;

        public ValidateLicenseCommand(PatcherContext context)
        {
            _context = context;
        }

        public void Execute(CancellationToken cancellationToken)
        {
            // TODO: Implementation :-)
            KeySecret = null;
        }

        public void Prepare(IProgressMonitor progressMonitor)
        {
            throw new System.NotImplementedException();
        }

        public string KeySecret { get; private set; }
    }
}