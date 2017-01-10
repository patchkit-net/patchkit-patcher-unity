using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    internal class AppUpdater
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdater));

        private readonly IAppUpdaterStrategyResolver _strategyResolver;
        private readonly AppUpdaterContext _context;

        private bool _patchCalled;

        public AppUpdater(string appSecret, string appDataPath, AppUpdaterConfiguration configuration) : this(
            new AppUpdaterStrategyResolver(), new AppUpdaterContext(appSecret, appDataPath, configuration))
        {
        }

        public AppUpdater(IAppUpdaterStrategyResolver strategyResolver, AppUpdaterContext context)
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