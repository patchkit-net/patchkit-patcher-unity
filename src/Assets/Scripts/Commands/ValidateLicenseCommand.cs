using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Status;

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

        public void Prepare(IStatusMonitor statusMonitor)
        {
        }

        public string KeySecret { get; private set; }
    }
}