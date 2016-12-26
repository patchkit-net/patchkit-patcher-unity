using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher
{
    internal class Patcher
    {
        private readonly IPatcherStrategyResolver _strategyResolver;
        private readonly PatcherContext _context;

        public Patcher(IPatcherStrategyResolver strategyResolver, PatcherContext context)
        {
            _strategyResolver = strategyResolver;
            _context = context;
        }

        public void Patch(CancellationToken cancellationToken)
        {
            var strategy = _strategyResolver.Resolve(_context);
            strategy.Patch(cancellationToken);
        }
    }
}