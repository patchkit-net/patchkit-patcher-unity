using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    internal class AppUpdater
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdater));

        private readonly IAppUpdaterStrategyResolver _strategyResolver;

        private bool _patchCalled;

        public readonly AppUpdaterContext Context;

        public AppUpdater(ILocalData localData, IRemoteData remoteData, AppUpdaterConfiguration configuration) : this(
            new AppUpdaterStrategyResolver(), new AppUpdaterContext(localData, remoteData, configuration))
        {
        }

        public AppUpdater(IAppUpdaterStrategyResolver strategyResolver, AppUpdaterContext context)
        {
            AssertChecks.ArgumentNotNull(strategyResolver, "strategyResolver");
            AssertChecks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _strategyResolver = strategyResolver;
            Context = context;
        }

        public void Patch(CancellationToken cancellationToken)
        {
            DebugLogger.Log("Patching.");

            AssertChecks.MethodCalledOnlyOnce(ref _patchCalled, "Patch");

            var strategy = _strategyResolver.Resolve(Context);
            strategy.Patch(cancellationToken);
        }
    }
}