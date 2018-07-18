﻿using System;
using System.IO;
using System.Threading;
using PatchKit.Unity.Utilities;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.AppUpdater.Commands;

namespace PatchKit.Unity.Patcher.AppUpdater
{
    public class AppUpdater
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppUpdater));

        public readonly AppUpdaterContext Context;

        private IAppUpdaterStrategyResolver _strategyResolver;

        private IAppUpdaterStrategy _strategy;

        private bool _updateHasBeenCalled;

        public AppUpdater(AppUpdaterContext context)
        {
            Checks.ArgumentNotNull(context, "context");

            DebugLogger.LogConstructor();

            _strategyResolver = new AppUpdaterStrategyResolver();
            Context = context;
        }

        public void Update(CancellationToken cancellationToken)
        {
            Assert.MethodCalledOnlyOnce(ref _updateHasBeenCalled, "Update");

            DebugLogger.Log("Updating.");

            StrategyType type = _strategyResolver.Resolve(Context);
            _strategy = _strategyResolver.Create(type, Context);
            Context.StatusMonitor.Reset();

            if (Context.Configuration.CreateDesktopShortcut && Platform.IsWindows())
            {
                try
                {
                    var launcherPath = Path.GetFullPath(LauncherUtilities.FindLauncherExecutable(PlatformType.Windows));
                    var appName = Patcher.Instance.AppInfo.Value.Name;

                    var iconLocation = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "elsword_192.ico"));

                    var createDesktopShortcut = new CreateDesktopShortcutCommand(appName, launcherPath, iconLocation);

                    createDesktopShortcut.Prepare(Context.StatusMonitor);
                    createDesktopShortcut.Execute(cancellationToken);
                }
                catch (Exception e)
                {
                    DebugLogger.LogException(e);
                }
            }

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

        private bool TryHandleFallback(CancellationToken cancellationToken)
        {
            var fallbackType = _strategyResolver.GetFallbackStrategy(_strategy.GetStrategyType());

            if (fallbackType == StrategyType.None)
            {
                return false;
            }

            Context.App.ReloadTemporaryDirectories(); // FIX: Bug #724

            _strategy = _strategyResolver.Create(fallbackType, Context);

            _strategy.Update(cancellationToken);

            return true;
        }
    }
}