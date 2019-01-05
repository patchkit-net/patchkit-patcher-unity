using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using PatchKit.Api;
using PatchKit.Apps.Updating;
using PatchKit.Apps.Updating.AppUpdater;
using PatchKit.Apps.Updating.Utilities;
using PatchKit.Core.IO.FileSystem;
using UnityEngine;

namespace PatchKit.Patching.Unity
{
    public partial class Patcher
    {
        private IEnumerator KillThread()
        {
            if (_isThreadBeingKilled)
            {
                yield break;
            }

            _isThreadBeingKilled = true;

            _debugLogger.Log("Killing patcher thread...");

            yield return StartCoroutine(KillThreadInner());

            _debugLogger.Log("Patcher thread has been killed.");

            _isThreadBeingKilled = false;
        }

        private IEnumerator KillThreadInner()
        {
            if (_thread == null)
            {
                yield break;
            }

            while (_thread.IsAlive)
            {
                CancelThread();

                float startWaitTime = Time.unscaledTime;
                while (Time.unscaledTime - startWaitTime < 1.0f && _thread.IsAlive)
                {
                    yield return null;
                }

                if (!_thread.IsAlive)
                {
                    break;
                }

                InterruptThread();

                startWaitTime = Time.unscaledTime;
                while (Time.unscaledTime - startWaitTime < 1.0f && _thread.IsAlive)
                {
                    yield return null;
                }

                if (!_thread.IsAlive)
                {
                    break;
                }

                AbortThread();

                startWaitTime = Time.unscaledTime;
                while (Time.unscaledTime - startWaitTime < 1.0f && _thread.IsAlive)
                {
                    yield return null;
                }
            }
        }

        private void StartThread()
        {
            _debugLogger.Log("Starting patcher thread...");

            _thread = new Thread(() => ThreadExecution(_threadCancellationTokenSource.Token));
            _thread.Start();
        }

        private void CancelThread()
        {
            _debugLogger.Log("Cancelling patcher thread...");

            _threadCancellationTokenSource.Cancel();
        }

        private void InterruptThread()
        {
            _debugLogger.Log("Interrupting patcher thread...");

            _thread.Interrupt();
        }

        private void AbortThread()
        {
            _debugLogger.Log("Aborting patcher thread...");

            _thread.Abort();
        }

        private void ThreadExecution(CancellationToken cancellationToken)
        {
            try
            {
                _state.Value = PatcherState.None;

                _debugLogger.Log("Patcher thread started.");

                try
                {
                    ThreadLoadPatcherData();
                }
                catch (NonLauncherExecutionException)
                {
                    try
                    {
                        LauncherUtilities.ExecuteLauncher();
                    }
                    catch (ApplicationException)
                    {
                        ThreadDisplayError(PatcherError.NonLauncherExecution, cancellationToken);
                    }
                    finally
                    {
                        Quit();
                    }
                }

                EnsureSingleInstance();

                ThreadLoadPatcherConfiguration();

                UnityDispatcher.Invoke(() => _app = new App(new Path(_data.Value.AppDataPath), _data.Value.AppSecret,
                    _data.Value.OverrideLatestVersionId)).WaitOne();

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ThreadWaitForUserDecision(cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    ThreadExecuteUserDecision(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _debugLogger.Log("Patcher thread finished: thread has been cancelled.");
            }
            catch (ThreadInterruptedException)
            {
                _debugLogger.Log("Patcher thread finished: thread has been interrupted.");
            }
            catch (ThreadAbortException)
            {
                _debugLogger.Log("Patcher thread finished: thread has been aborted.");
            }
            catch (MultipleInstancesException exception)
            {
                _debugLogger.LogException(exception);
                Quit();
            }
            catch (Exception exception)
            {
                _debugLogger.LogError("Patcher thread failed: an exception has occured.");
                _debugLogger.LogException(exception);
            }
            finally
            {
                _state.Value = PatcherState.None;
            }
        }

        private void ThreadLoadPatcherData()
        {
            try
            {
                _debugLogger.Log("Loading patcher data...");
                _state.Value = PatcherState.LoadingPatcherData;

#if UNITY_EDITOR
                UnityDispatcher.Invoke(() =>
                {
                    _debugLogger.Log("Using Unity Editor patcher data.");
                    _data.Value = new PatcherData
                    {
                        AppSecret = EditorAppSecret,
                        AppDataPath =
                            Application.dataPath.Replace("/Assets",
                                $"/Temp/PatcherApp{EditorAppSecret}"),
                        OverrideLatestVersionId = EditorOverrideLatestVersionId
                    };
                }).WaitOne();
#else
                DebugLogger.Log("Using command line patcher data reader.");
                var inputArgumentsPatcherDataReader = new InputArgumentsPatcherDataReader();
                _data.Value = inputArgumentsPatcherDataReader.Read();
#endif
                _debugLogger.LogVariable(_data.Value.AppSecret, "Data.AppSecret");
                _debugLogger.LogVariable(_data.Value.AppDataPath, "Data.AppDataPath");
                _debugLogger.LogVariable(_data.Value.OverrideLatestVersionId, "Data.OverrideLatestVersionId");
                _debugLogger.LogVariable(_data.Value.LockFilePath, "Data.LockFilePath");

                _debugLogger.Log("Patcher data loaded.");
            }
            catch (ThreadInterruptedException)
            {
                _debugLogger.Log(
                    "Loading patcher data interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                _debugLogger.Log("Loading patcher data aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                _debugLogger.LogError(
                    "Error while loading patcher data: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void ThreadLoadPatcherConfiguration()
        {
            try
            {
                _debugLogger.Log("Loading patcher configuration...");

                _state.Value = PatcherState.LoadingPatcherConfiguration;

                // TODO: Use PatcherConfigurationReader
                _configuration = DefaultConfiguration;

                _debugLogger.Log("Patcher configuration loaded.");
            }
            catch (ThreadInterruptedException)
            {
                _debugLogger.Log(
                    "Loading patcher configuration interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                _debugLogger.Log(
                    "Loading patcher configuration aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                _debugLogger.LogError(
                    "Error while loading patcher configuration: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void ThreadWaitForUserDecision(CancellationToken cancellationToken)
        {
            try
            {
                _debugLogger.Log("Waiting for user decision...");

                _state.Value = PatcherState.WaitingForUserDecision;

                bool isInstalled = _app.IsInstalled();

                _debugLogger.LogVariable(isInstalled, nameof(isInstalled));

                _canRepairApp.Value = isInstalled;
                _canInstallApp.Value = !isInstalled;
                _canCheckForAppUpdates.Value = isInstalled;
                _canStartApp.Value = isInstalled;

                if (_canInstallApp.Value && _configuration.AutomaticallyInstallApp && !_hasAutomaticallyInstalledApp)
                {
                    _debugLogger.Log("Automatically deciding to install app.");
                    _hasAutomaticallyInstalledApp = true;
                    _hasAutomaticallyCheckedForAppUpdate = true;
                    _userDecision = UserDecision.InstallAppAutomatically;
                    return;
                }

                if (!_hasAutomaticallyCheckVersionIntegrity && _configuration.AutomaticallyCheckForVersionIntegrity)
                {
                    _debugLogger.Log("Automatically deciding to check version integrity.");
                    _hasAutomaticallyCheckVersionIntegrity = true;
                    _userDecision = UserDecision.CheckIntegrityAutomatically;
                    return;
                }

                if (_canCheckForAppUpdates.Value && _configuration.AutomaticallyCheckForAppUpdates &&
                    !_hasAutomaticallyCheckedForAppUpdate)
                {
                    _debugLogger.Log("Automatically deciding to check for app updates.");
                    _hasAutomaticallyInstalledApp = true;
                    _hasAutomaticallyCheckedForAppUpdate = true;
                    _userDecision = UserDecision.CheckForAppUpdatesAutomatically;
                    return;
                }

                if (_canStartApp.Value && _configuration.AutomaticallyStartApp && !_hasAutomaticallyStartedApp)
                {
                    _debugLogger.Log("Automatically deciding to start app.");
                    _hasAutomaticallyStartedApp = true;
                    _userDecision = UserDecision.StartAppAutomatically;
                    return;
                }

                _userDecisionSetEvent.Reset();
                using (cancellationToken.Register(() => _userDecisionSetEvent.Set()))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _userDecisionSetEvent.WaitOne();
                }

                cancellationToken.ThrowIfCancellationRequested();

                _debugLogger.Log($"Waiting for user decision result: {_userDecision}.");
            }
            catch (OperationCanceledException)
            {
                _debugLogger.Log("Waiting for user decision cancelled.");
            }
            catch (ThreadInterruptedException)
            {
                _debugLogger.Log(
                    "Waiting for user decision interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                _debugLogger.Log("Waiting for user decision aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                _debugLogger.LogWarning(
                    "Error while waiting for user decision: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void ThreadExecuteUserDecision(CancellationToken cancellationToken)
        {
            bool displayWarningInsteadOfError = false;

            try
            {
                _warning.Value = string.Empty;

                _debugLogger.Log($"Executing user decision {_userDecision}...");

                switch (_userDecision)
                {
                    case UserDecision.None:
                        break;
                    case UserDecision.StartAppAutomatically:
                    case UserDecision.StartApp:
                        ThreadStartApp();
                        break;
                    case UserDecision.InstallAppAutomatically:
                        displayWarningInsteadOfError = _app.IsInstalled();
                        ThreadUpdateApp(true, cancellationToken);
                        break;
                    case UserDecision.InstallApp:
                        ThreadUpdateApp(false, cancellationToken);
                        break;
                    case UserDecision.CheckForAppUpdatesAutomatically:
                        displayWarningInsteadOfError = _app.IsInstalled();
                        ThreadUpdateApp(true, cancellationToken);
                        break;
                    case UserDecision.CheckForAppUpdates:
                        ThreadUpdateApp(false, cancellationToken);
                        break;
                    case UserDecision.CheckIntegrityAutomatically:
                    case UserDecision.CheckIntegrity:
                        break;
                }

                _debugLogger.Log($"User decision {_userDecision} execution done.");
            }
            catch (OperationCanceledException)
            {
                _debugLogger.Log($"User decision {_userDecision} execution cancelled.");
            }
            catch (UnauthorizedAccessException e)
            {
                _debugLogger.Log($"User decision {_userDecision} execution issue: permissions failure.");
                _debugLogger.LogException(e);

                if (ThreadTryRestartWithRequestForPermissions())
                {
                    UnityDispatcher.Invoke(Quit);
                }
                else
                {
                    ThreadDisplayError(PatcherError.NoPermissions, cancellationToken);
                }
            }
            catch (ApiConnectionException e)
            {
                _debugLogger.LogException(e);

                if (displayWarningInsteadOfError)
                {
                    _warning.Value = "Unable to check for updates. Please check your internet connection.";
                }
                else
                {
                    ThreadDisplayError(PatcherError.NoInternetConnection, cancellationToken);
                }
            }
            catch (ThreadInterruptedException)
            {
                _debugLogger.Log(
                    $"User decision {_userDecision} execution interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                _debugLogger.Log(
                    $"User decision {_userDecision} execution aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception exception)
            {
                _debugLogger.LogWarning(
                    $"Error while executing user decision {_userDecision}: an exception has occured.");
                _debugLogger.LogException(exception);

                if (displayWarningInsteadOfError)
                {
                    _warning.Value = "Unable to check for updates. Please check your internet connection.";
                }
                else
                {
                    ThreadDisplayError(PatcherError.Other, cancellationToken);
                }
            }
        }

        private void ThreadDisplayError(PatcherError error, CancellationToken cancellationToken)
        {
            try
            {
                _debugLogger.Log($"Displaying patcher error {error}...");

                ErrorDialog.Display(error, cancellationToken);

                _debugLogger.Log($"Patcher error {error} displayed.");
            }
            catch (OperationCanceledException)
            {
                _debugLogger.Log($"Displaying patcher error {_userDecision} cancelled.");
            }
            catch (ThreadInterruptedException)
            {
                _debugLogger.Log(
                    $"Displaying patcher error {error} interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                _debugLogger.Log(
                    $"Displaying patcher error {error} aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                _debugLogger.LogWarning(
                    $"Error while displaying patcher error {error}: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void ThreadStartApp()
        {
            _state.Value = PatcherState.StartingApp;

            var appStarter = new AppStarter(_app);

            appStarter.Start();

            UnityDispatcher.Invoke(Quit);
        }

        private void ThreadUpdateApp(bool automatically, CancellationToken cancellationToken)
        {
            _state.Value = PatcherState.UpdatingApp;

            _appInfo.Value = _app.GetAppInfo(cancellationToken);
            _remoteVersionId.Value = _app.GetLatestVersionId(!automatically);
            if (_app.IsInstalled())
            {
                _localVersionId.Value = _app.GetInstalledVersionId();
            }

            _updateAppCancellationTokenSource = new CancellationTokenSource();

            using (cancellationToken.Register(() => _updateAppCancellationTokenSource.Cancel()))
            {
                var appUpdater = new AppUpdater(new Context(_app, _configuration.AppUpdaterConfiguration));

                try
                {
                    _updaterStatus.Value = appUpdater.Status;
                    appUpdater.Update(_updateAppCancellationTokenSource.Token);
                }
                finally
                {
                    _updaterStatus.Value = null;
                    _updateAppCancellationTokenSource = null;
                }
            }
        }

        private bool ThreadTryRestartWithRequestForPermissions()
        {
            _debugLogger.Log("Restarting patcher with request for permissions.");

            try
            {
                RuntimePlatform applicationPlatform = default(RuntimePlatform);
                string applicationDataPath = string.Empty;

                UnityDispatcher.Invoke(() =>
                {
                    applicationPlatform = Application.platform;
                    applicationDataPath = Application.dataPath;
                }).WaitOne();

                if (applicationPlatform == RuntimePlatform.WindowsPlayer)
                {
                    var info = new ProcessStartInfo
                    {
                        FileName = applicationDataPath.Replace("_Data", ".exe"),
                        Arguments =
                            string.Join(" ", Environment.GetCommandLineArgs().Select(s => "\"" + s + "\"").ToArray()),
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                    Process.Start(info);

                    _debugLogger.Log("Patcher restarted with request for permissions.");

                    return true;
                }

                _debugLogger.Log(
                    $"Restarting patcher with request for permissions not possible: unsupported platform {applicationPlatform}.");

                return false;
            }
            catch (ThreadInterruptedException)
            {
                _debugLogger.Log(
                    "Restarting patcher with request for permissions interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                _debugLogger.Log(
                    "Restarting patcher with request for permissions aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception exception)
            {
                _debugLogger.LogWarning(
                    "Error while restarting patcher with request for permissions: an exception has occured.");
                _debugLogger.LogException(exception);

                return false;
            }
        }
    }
}