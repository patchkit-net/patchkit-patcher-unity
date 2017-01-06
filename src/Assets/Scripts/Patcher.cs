using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher
{
    internal class Patcher
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(Patcher));

        private readonly IPatcherStrategyResolver _strategyResolver;
        private readonly PatcherContext _context;

        private bool _patchCalled;

        public Patcher(IPatcherStrategyResolver strategyResolver, PatcherContext context)
        {
            AssertChecks.ArgumentNotNull(strategyResolver, "strategyResolver");
            AssertChecks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _strategyResolver = strategyResolver;
            _context = context;
        }

        public void Patch(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Patching.");

            AssertChecks.MethodCalledOnlyOnce(ref _patchCalled, "Patch");

            var strategy = _strategyResolver.Resolve(_context);
            strategy.Patch(cancellationToken);
        }
    }
}