using PatchKit.Unity.Patcher.Cancellation;

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

        public string KeySecret { get; private set; }
    }
}
