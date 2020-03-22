using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Collections.Generic;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.AppUpdater.Commands;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Api.Models.Main;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdater
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdater));

        public readonly AppUpdaterContext Context;

        private readonly UpdaterStatus _status = new UpdaterStatus();

        private IAppUpdaterStrategyResolver _strategyResolver;

        private IAppUpdaterStrategy _strategy;

        private bool _updateHasBeenCalled;

        public IReadOnlyUpdaterStatus Status
        {
            get { return _status; }
        }

        public AppUpdater(AppUpdaterContext context)
        {
            Checks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _strategyResolver = new AppUpdaterStrategyResolver(_status);
            Context = context;
        }

        private void PreUpdate(PatchKit.Unity.Patcher.Cancellation.CancellationToken cancellationToken)
        {
            var appRepairer = new AppRepairer(Context, _status);
            appRepairer.Perform(cancellationToken);
        }

        public void Update(PatchKit.Unity.Patcher.Cancellation.CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _updateHasBeenCalled, "Update");

            if (Context.App.IsInstallationBroken() || Context.App.IsFullyInstalled())
            {
                PreUpdate(cancellationToken);
            }

            DebugLogger.Log("Updating.");

            StrategyType type = _strategyResolver.Resolve(Context, cancellationToken);
            _strategy = _strategyResolver.Create(type, Context);

            try
            {
                _strategy.Update(cancellationToken);
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException
                    || ex is UnauthorizedAccessException
                    || ex is NotEnoughtDiskSpaceException
                    || ex is ThreadInterruptedException
                    || ex is ThreadAbortException)
                {
                    DebugLogger.LogWarning("Strategy caused exception, to be handled further");
                    throw;
                }
                else
                {
                    DebugLogger.LogWarningFormat("Strategy caused exception, being handled by fallback: {0}, Trace: {1}", ex, ex.StackTrace);

                    if (!TryHandleFallback(cancellationToken))
                    {
                        throw;
                    }
                }
            }
        }

        private bool TryHandleFallback(PatchKit.Unity.Patcher.Cancellation.CancellationToken cancellationToken)
        {
            var fallbackType = _strategyResolver.GetFallbackStrategy(_strategy.GetStrategyType());

            if (fallbackType == StrategyType.None)
            {
                return false;
            }

            _strategy = _strategyResolver.Create(fallbackType, Context);

            _strategy.Update(cancellationToken);

            return true;
        }
    }
}