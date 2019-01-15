using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using PatchKit.Api;
using PatchKit.Apps;
using PatchKit.Apps.Updating;
using PatchKit.Apps.Updating.AppUpdater;
using PatchKit.Apps.Updating.AppUpdater.Status;
using PatchKit.Apps.Updating.Utilities;
using PatchKit.Core;
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

            UnityEngine.Debug.Log("Killing patcher thread...");

            yield return StartCoroutine(KillThreadInner());

            UnityEngine.Debug.Log("Patcher thread has been killed.");

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
            UnityEngine.Debug.Log("Starting patcher thread...");

            _thread = new Thread(() => ThreadExecution(_threadCancellationTokenSource.Token));
            _thread.Start();
        }

        private void CancelThread()
        {
            UnityEngine.Debug.Log("Cancelling patcher thread...");

            _threadCancellationTokenSource.Cancel();
        }

        private void InterruptThread()
        {
            UnityEngine.Debug.Log("Interrupting patcher thread...");

            _thread.Interrupt();
        }

        private void AbortThread()
        {
            UnityEngine.Debug.Log("Aborting patcher thread...");

            _thread.Abort();
        }

        private void ThreadExecution(CancellationToken cancellationToken)
        {
            try
            {
                _state.Value = PatcherState.None;

                UnityEngine.Debug.Log("Patcher thread started.");

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

                UnityDispatcher.Invoke(() =>
                {
                    _app = new App(new Path(_data.Value.AppDataPath));
                    _remoteApp = new RemoteApp(new AppSecret(_data.Value.AppSecret),
                        new AppLicenseKey(new Text("asd")));
                }).WaitOne();

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
                UnityEngine.Debug.Log("Patcher thread finished: thread has been cancelled.");
            }
            catch (ThreadInterruptedException)
            {
                UnityEngine.Debug.Log("Patcher thread finished: thread has been interrupted.");
            }
            catch (ThreadAbortException)
            {
                UnityEngine.Debug.Log("Patcher thread finished: thread has been aborted.");
            }
            catch (MultipleInstancesException exception)
            {
                UnityEngine.Debug.Log(exception);
                Quit();
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError("Patcher thread failed: an exception has occured.");
                UnityEngine.Debug.LogException(exception);
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
                UnityEngine.Debug.Log("Loading patcher data...");
                _state.Value = PatcherState.LoadingPatcherData;

#if UNITY_EDITOR
                UnityDispatcher.Invoke(() =>
                {
                    UnityEngine.Debug.Log("Using Unity Editor patcher data.");
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
                UnityEngine.Debug.Log("Data.AppSecret = " + _data.Value.AppSecret);
                UnityEngine.Debug.Log("Data.AppDataPath = " + _data.Value.AppDataPath);
                UnityEngine.Debug.Log("Data.OverrideLatestVersionId = " + _data.Value.OverrideLatestVersionId);
                UnityEngine.Debug.Log("Data.LockFilePath = " + _data.Value.LockFilePath);

                UnityEngine.Debug.Log("Patcher data loaded.");
            }
            catch (ThreadInterruptedException)
            {
                UnityEngine.Debug.Log(
                    "Loading patcher data interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                UnityEngine.Debug.Log("Loading patcher data aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                UnityEngine.Debug.LogError(
                    "Error while loading patcher data: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void ThreadLoadPatcherConfiguration()
        {
            try
            {
                UnityEngine.Debug.Log("Loading patcher configuration...");

                _state.Value = PatcherState.LoadingPatcherConfiguration;

                // TODO: Use PatcherConfigurationReader
                _configuration = DefaultConfiguration;

                UnityEngine.Debug.Log("Patcher configuration loaded.");
            }
            catch (ThreadInterruptedException)
            {
                UnityEngine.Debug.Log(
                    "Loading patcher configuration interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                UnityEngine.Debug.Log(
                    "Loading patcher configuration aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                UnityEngine.Debug.LogError(
                    "Error while loading patcher configuration: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void ThreadWaitForUserDecision(CancellationToken cancellationToken)
        {
            try
            {
                UnityEngine.Debug.Log("Waiting for user decision...");

                _state.Value = PatcherState.WaitingForUserDecision;

                bool isInstalled = DependencyResolver.Resolve<IsAppInstalledDelegate>()(_app);

                UnityEngine.Debug.Log(nameof(isInstalled) + " = " + isInstalled);

                _canRepairApp.Value = isInstalled;
                _canInstallApp.Value = !isInstalled;
                _canCheckForAppUpdates.Value = isInstalled;
                _canStartApp.Value = isInstalled;

                if (_canInstallApp.Value && _configuration.AutomaticallyInstallApp && !_hasAutomaticallyInstalledApp)
                {
                    UnityEngine.Debug.Log("Automatically deciding to install app.");
                    _hasAutomaticallyInstalledApp = true;
                    _hasAutomaticallyCheckedForAppUpdate = true;
                    _userDecision = UserDecision.InstallAppAutomatically;
                    return;
                }

                if (!_hasAutomaticallyCheckVersionIntegrity && _configuration.AutomaticallyCheckForVersionIntegrity)
                {
                    UnityEngine.Debug.Log("Automatically deciding to check version integrity.");
                    _hasAutomaticallyCheckVersionIntegrity = true;
                    _userDecision = UserDecision.CheckIntegrityAutomatically;
                    return;
                }

                if (_canCheckForAppUpdates.Value && _configuration.AutomaticallyCheckForAppUpdates &&
                    !_hasAutomaticallyCheckedForAppUpdate)
                {
                    UnityEngine.Debug.Log("Automatically deciding to check for app updates.");
                    _hasAutomaticallyInstalledApp = true;
                    _hasAutomaticallyCheckedForAppUpdate = true;
                    _userDecision = UserDecision.CheckForAppUpdatesAutomatically;
                    return;
                }

                if (_canStartApp.Value && _configuration.AutomaticallyStartApp && !_hasAutomaticallyStartedApp)
                {
                    UnityEngine.Debug.Log("Automatically deciding to start app.");
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

                UnityEngine.Debug.Log($"Waiting for user decision result: {_userDecision}.");
            }
            catch (OperationCanceledException)
            {
                UnityEngine.Debug.Log("Waiting for user decision cancelled.");
            }
            catch (ThreadInterruptedException)
            {
                UnityEngine.Debug.Log(
                    "Waiting for user decision interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                UnityEngine.Debug.Log(
                    "Waiting for user decision aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                UnityEngine.Debug.LogWarning(
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

                UnityEngine.Debug.Log($"Executing user decision {_userDecision}...");

                switch (_userDecision)
                {
                    case UserDecision.None:
                        break;
                    case UserDecision.StartAppAutomatically:
                    case UserDecision.StartApp:
                        ThreadStartApp();
                        break;
                    case UserDecision.InstallAppAutomatically:
                        displayWarningInsteadOfError = DependencyResolver.Resolve<IsAppInstalledDelegate>()(_app);
                        ThreadUpdateApp(true, cancellationToken);
                        break;
                    case UserDecision.InstallApp:
                        ThreadUpdateApp(false, cancellationToken);
                        break;
                    case UserDecision.CheckForAppUpdatesAutomatically:
                        displayWarningInsteadOfError = DependencyResolver.Resolve<IsAppInstalledDelegate>()(_app);
                        ThreadUpdateApp(true, cancellationToken);
                        break;
                    case UserDecision.CheckForAppUpdates:
                        ThreadUpdateApp(false, cancellationToken);
                        break;
                    case UserDecision.CheckIntegrityAutomatically:
                    case UserDecision.CheckIntegrity:
                        break;
                }

                UnityEngine.Debug.Log($"User decision {_userDecision} execution done.");
            }
            catch (OperationCanceledException)
            {
                UnityEngine.Debug.Log($"User decision {_userDecision} execution cancelled.");
            }
            catch (UnauthorizedAccessException e)
            {
                UnityEngine.Debug.Log($"User decision {_userDecision} execution issue: permissions failure.");
                UnityEngine.Debug.LogException(e);

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
                UnityEngine.Debug.LogException(e);

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
                UnityEngine.Debug.Log(
                    $"User decision {_userDecision} execution interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                UnityEngine.Debug.Log(
                    $"User decision {_userDecision} execution aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning(
                    $"Error while executing user decision {_userDecision}: an exception has occured.");
                UnityEngine.Debug.LogException(exception);

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
                UnityEngine.Debug.Log($"Displaying patcher error {error}...");

                ErrorDialog.Display(error, cancellationToken);

                UnityEngine.Debug.Log($"Patcher error {error} displayed.");
            }
            catch (OperationCanceledException)
            {
                UnityEngine.Debug.Log($"Displaying patcher error {_userDecision} cancelled.");
            }
            catch (ThreadInterruptedException)
            {
                UnityEngine.Debug.Log(
                    $"Displaying patcher error {error} interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                UnityEngine.Debug.Log(
                    $"Displaying patcher error {error} aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                UnityEngine.Debug.LogWarning(
                    $"Error while displaying patcher error {error}: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void ThreadStartApp()
        {
            _state.Value = PatcherState.StartingApp;

            DependencyResolver.Resolve<StartAppDelegate>()(_app);

            UnityDispatcher.Invoke(Quit);
        }

        private void ThreadUpdateApp(bool automatically, CancellationToken cancellationToken)
        {
            _state.Value = PatcherState.UpdatingApp;

            _appInfo.Value = DependencyResolver.Resolve<IApiConnection>()
                .GetApplicationInfo(_remoteApp.Secret, null, cancellationToken);
            _remoteVersionId.Value = DependencyResolver.Resolve<IApiConnection>()
                .GetAppLatestAppVersionId(_remoteApp.Secret, null, cancellationToken).Id;
            if (DependencyResolver.Resolve<IsAppInstalledDelegate>()(_app))
            {
                _localVersionId.Value = DependencyResolver.Resolve<GetAppInstalledVersionIdDelegate>()(_app);
            }

            _updateAppCancellationTokenSource = new CancellationTokenSource();

            using (cancellationToken.Register(() => _updateAppCancellationTokenSource.Cancel()))
            {
                var updaterStatus = new UpdaterStatus();

                try
                {
                    _updaterStatus.Value = updaterStatus;

                    var op = new DownloadStatus();
                    op.Weight.Value = 1.0;
                    op.IsActive.Value = true;
                    op.Description.Value = "Installing...";

                    updaterStatus.RegisterOperation(op);

                    DependencyResolver.Resolve<UpdateDelegate>()(_app, _remoteApp,
                        new VersionId(_remoteVersionId.Value.Value),
                        p =>
                        {
                            op.TotalBytes.Value = p.TotalBytes;
                            op.Bytes.Value = p.InstalledBytes;
                        }, _updateAppCancellationTokenSource.Token);
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
            UnityEngine.Debug.Log("Restarting patcher with request for permissions.");

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

                    UnityEngine.Debug.Log("Patcher restarted with request for permissions.");

                    return true;
                }

                UnityEngine.Debug.Log(
                    $"Restarting patcher with request for permissions not possible: unsupported platform {applicationPlatform}.");

                return false;
            }
            catch (ThreadInterruptedException)
            {
                UnityEngine.Debug.Log(
                    "Restarting patcher with request for permissions interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                UnityEngine.Debug.Log(
                    "Restarting patcher with request for permissions aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning(
                    "Error while restarting patcher with request for permissions: an exception has occured.");
                UnityEngine.Debug.LogException(exception);

                return false;
            }
        }
    }
}