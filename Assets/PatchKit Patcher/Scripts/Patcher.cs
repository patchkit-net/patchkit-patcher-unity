﻿using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using PatchKit.Api;
using PatchKit.Apps.Updating;
using PatchKit.Apps.Updating.AppData.Local;
using PatchKit.Apps.Updating.AppData.Remote;
using PatchKit.Apps.Updating.AppData.Remote.Downloaders;
using PatchKit.Apps.Updating.AppUpdater;
using PatchKit.Apps.Updating.AppUpdater.Commands;
using PatchKit.Apps.Updating.AppUpdater.Status;
using PatchKit.Apps.Updating.Debug;
using PatchKit.Apps.Updating.Licensing;
using PatchKit.Apps.Updating.Utilities;
using PatchKit.Core.Cancellation;
using PatchKit.Core.IO;
using PatchKit.Network;
using PatchKit.Patching.Unity.UI.Dialogs;
using UniRx;
using UnityEngine;
using CancellationToken = PatchKit.Core.Cancellation.CancellationToken;

namespace PatchKit.Patching.Unity
{
    // Assumptions:
    // - this component is always enabled (coroutines are always executed)
    // - this component is destroyed only when application quits
    public class Patcher : MonoBehaviour
    {
        public const string EditorAllowedSecret = "ac20fc855b75a7ea5f3e936dfd38ccd8";

        public enum UserDecision
        {
            None,
            RepairApp,
            StartApp,
            StartAppAutomatically,
            InstallApp,
            InstallAppAutomatically,
            CheckForAppUpdates,
            CheckForAppUpdatesAutomatically
        }

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(Patcher));

        private static Patcher _instance;

        public static Patcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<Patcher>();
                }
                return _instance;
            }
        }

        private bool _canStartThread = true;

        private readonly CancellationTokenSource _threadCancellationTokenSource = new CancellationTokenSource();

        private Thread _thread;

        private bool _isThreadBeingKilled;

        private App _app;

        private PatcherConfiguration _configuration;

        private UserDecision _userDecision = UserDecision.None;

        private readonly ManualResetEvent _userDecisionSetEvent = new ManualResetEvent(false);

        private readonly IRequestTimeoutCalculator _requestTimeoutCalculator = new SimpleRequestTimeoutCalculator();
        
        private bool _hasAutomaticallyInstalledApp;

        private bool _hasAutomaticallyCheckedForAppUpdate;

        private bool _hasAutomaticallyStartedApp;

        private FileStream _lockFileStream;

        private CancellationTokenSource _updateAppCancellationTokenSource;

        public ErrorDialog ErrorDialog;

        public string EditorAppSecret;

        public int EditorOverrideLatestVersionId;

        public PatcherConfiguration DefaultConfiguration;

        private readonly ReactiveProperty<IReadOnlyUpdaterStatus> _updaterStatus = new ReactiveProperty<IReadOnlyUpdaterStatus>();

        public IReadOnlyReactiveProperty<IReadOnlyUpdaterStatus> UpdaterStatus
        {
            get { return _updaterStatus; }
        }

        private readonly BoolReactiveProperty _canRepairApp = new BoolReactiveProperty(false);

        public IReadOnlyReactiveProperty<bool> CanRepairApp
        {
            get { return _canRepairApp; }
        }

        private readonly BoolReactiveProperty _canStartApp = new BoolReactiveProperty(false);

        public IReadOnlyReactiveProperty<bool> CanStartApp
        {
            get { return _canStartApp; }
        }

        private readonly BoolReactiveProperty _canInstallApp = new BoolReactiveProperty(false);

        public IReadOnlyReactiveProperty<bool> CanInstallApp
        {
            get { return _canInstallApp; }
        }

        private readonly BoolReactiveProperty _canCheckForAppUpdates = new BoolReactiveProperty(false);

        public IReadOnlyReactiveProperty<bool> CanCheckForAppUpdates
        {
            get { return _canCheckForAppUpdates; }
        }

        private readonly ReactiveProperty<PatcherState> _state = new ReactiveProperty<PatcherState>(PatcherState.None);

        public IReadOnlyReactiveProperty<PatcherState> State
        {
            get { return _state; }
        }

        private readonly ReactiveProperty<PatcherData> _data = new ReactiveProperty<PatcherData>();

        public IReadOnlyReactiveProperty<PatcherData> Data
        {
            get { return _data; }
        }
        
        private readonly ReactiveProperty<string> _warning = new ReactiveProperty<string>();

        public IReadOnlyReactiveProperty<string> Warning
        {
            get { return _warning; }
        }

        private readonly ReactiveProperty<int?> _remoteVersionId = new ReactiveProperty<int?>();

        public IReadOnlyReactiveProperty<int?> RemoteVersionId
        {
            get { return _remoteVersionId; }
        }

        private readonly ReactiveProperty<int?> _localVersionId = new ReactiveProperty<int?>();

        public IReadOnlyReactiveProperty<int?> LocalVersionId
        {
            get { return _localVersionId; }
        }

        private readonly ReactiveProperty<Api.Models.Main.App> _appInfo = new ReactiveProperty<Api.Models.Main.App>();

        public IReadOnlyReactiveProperty<Api.Models.Main.App> AppInfo
        {
            get { return _appInfo; }
        }

        public void SetUserDecision(UserDecision userDecision)
        {
            DebugLogger.Log(string.Format("User deicision set to {0}.", userDecision));

            _userDecision = userDecision;
            _userDecisionSetEvent.Set();
        }

        public void CancelUpdateApp()
        {
            if (_updateAppCancellationTokenSource != null)
            {
                DebugLogger.Log("Cancelling update app execution.");

                _updateAppCancellationTokenSource.Cancel();
            }
        }

        public void Quit()
        {
            DebugLogger.Log("Quitting application.");
            _canStartThread = false;

#if UNITY_EDITOR
            if (Application.isEditor)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
            else
#endif
            {
                CloseLockFile();
                Application.Quit();
            }
        }

        private void CloseLockFile()
        {
            try
            { 
                if (_lockFileStream != null)
                {
                    _lockFileStream.Close();
                    
                    DebugLogger.Log("Deleting the lock file.");
                    File.Delete(_data.Value.LockFilePath);
                }
            }
            catch
            {
                DebugLogger.LogWarning("Lock file closing error");
            }
        }

        private void Awake()
        {
            UnityEngine.Assertions.Assert.raiseExceptions = true;

            DependencyResolver.RegisterInstance<IApiConnectionSettingsProvider>(Settings.FindInstance());
            DependencyResolver.RegisterType<IPlatformResolver, PlatformResolver>();
            DependencyResolver.RegisterType<ITorrentClientProcessStartInfoProvider, UnityTorrentClientProcessStartInfoProvider>();
            DependencyResolver.RegisterType<IHttpClient, UnityHttpClient>();
            DependencyResolver.RegisterType<ICache, UnityCache>();
            DependencyResolver.RegisterType<ITemporaryDirectoryExecutor, UnityTemporaryDirectoryExecutor>();

            Assert.IsNull(_instance, "There must be only one instance of Patcher component.");
            Assert.IsNotNull(ErrorDialog, "ErrorDialog must be set.");

            _instance = this;
            UnityDispatcher.Initialize();
            Application.runInBackground = true;

            DebugLogger.LogFormat("patchkit-patcher-unity: {0}", Version.Value);
            DebugLogger.LogFormat("System version: {0}", EnvironmentInfo.GetSystemVersion());
            DebugLogger.LogFormat("Runtime version: {0}", EnvironmentInfo.GetSystemVersion());

            CheckEditorAppSecretSecure();

            if (_canStartThread)
            {
                StartThread();
            }
        }

        /// <summary>
        /// During patcher testing somebody may replace the secret with real game secret. If that would happen,
        /// patcher should quit immediatelly with following error.
        /// </summary>
        private void CheckEditorAppSecretSecure()
        {
            if (!Application.isEditor)
            {
                if (!string.IsNullOrEmpty(EditorAppSecret) && EditorAppSecret.Trim() != EditorAllowedSecret)
                {
                    DebugLogger.LogError("Security issue: EditorAppSecret is set to not allowed value. " +
                                         "Please change it inside Unity editor to " + EditorAllowedSecret +
                                         " and build the project again.");
                    Quit();
                }
            }
        }

        private void Update()
        {
            if (_thread == null || !_thread.IsAlive)
            {
                DebugLogger.Log("Quitting application because patcher thread is not alive.");
                Quit();
            }
        }

        private void OnApplicationQuit()
        {
            if (_thread != null && _thread.IsAlive)
            {
                DebugLogger.Log("Cancelling application quit because patcher thread is alive.");

                Application.CancelQuit();
                
                StartCoroutine(KillThread());
            }
        }

        private IEnumerator KillThread()
        {
            if (_isThreadBeingKilled)
            {
                yield break;
            }

            _isThreadBeingKilled = true;

            DebugLogger.Log("Killing patcher thread...");

            yield return StartCoroutine(KillThreadInner());

            DebugLogger.Log("Patcher thread has been killed.");

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
            DebugLogger.Log("Starting patcher thread...");

            _thread = new Thread(() => ThreadExecution(_threadCancellationTokenSource.Token));
            _thread.Start();
        }

        private void CancelThread()
        {
            DebugLogger.Log("Cancelling patcher thread...");

            _threadCancellationTokenSource.Cancel();
        }

        private void InterruptThread()
        {
            DebugLogger.Log("Interrupting patcher thread...");

            _thread.Interrupt();
        }

        private void AbortThread()
        {
            DebugLogger.Log("Aborting patcher thread...");

            _thread.Abort();
        }

        private void ThreadExecution(CancellationToken cancellationToken)
        {
            try
            {
                _state.Value = PatcherState.None;

                DebugLogger.Log("Patcher thread started.");

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

                UnityDispatcher.Invoke(() => _app = new App(_data.Value.AppDataPath, _data.Value.AppSecret, _data.Value.OverrideLatestVersionId, _requestTimeoutCalculator)).WaitOne();

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
                DebugLogger.Log("Patcher thread finished: thread has been cancelled.");
            }
            catch (ThreadInterruptedException)
            {
                DebugLogger.Log("Patcher thread finished: thread has been interrupted.");
            }
            catch (ThreadAbortException)
            {
                DebugLogger.Log("Patcher thread finished: thread has been aborted.");
            }            
            catch (MultipleInstancesException exception)
            {
                DebugLogger.LogException(exception);
                Quit();                
            }
            catch (Exception exception)
            {
                DebugLogger.LogError("Patcher thread failed: an exception has occured.");
                DebugLogger.LogException(exception);
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
                DebugLogger.Log("Loading patcher data...");
                _state.Value = PatcherState.LoadingPatcherData;

#if UNITY_EDITOR
                UnityDispatcher.Invoke(() =>
                {
                    DebugLogger.Log("Using Unity Editor patcher data.");
                    _data.Value = new PatcherData
                    {
                        AppSecret = EditorAppSecret,
                        AppDataPath =
                            Application.dataPath.Replace("/Assets",
                                string.Format("/Temp/PatcherApp{0}", EditorAppSecret)),
                        OverrideLatestVersionId = EditorOverrideLatestVersionId
                    };
                }).WaitOne();
#else
                DebugLogger.Log("Using command line patcher data reader.");
                var inputArgumentsPatcherDataReader = new InputArgumentsPatcherDataReader();
                _data.Value = inputArgumentsPatcherDataReader.Read();
#endif
                DebugLogger.LogVariable(_data.Value.AppSecret, "Data.AppSecret");
                DebugLogger.LogVariable(_data.Value.AppDataPath, "Data.AppDataPath");
                DebugLogger.LogVariable(_data.Value.OverrideLatestVersionId, "Data.OverrideLatestVersionId");
                DebugLogger.LogVariable(_data.Value.LockFilePath, "Data.LockFilePath");

                DebugLogger.Log("Patcher data loaded.");
            }
            catch (ThreadInterruptedException)
            {
                DebugLogger.Log("Loading patcher data interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                DebugLogger.Log("Loading patcher data aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                DebugLogger.LogError("Error while loading patcher data: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void EnsureSingleInstance()
        {
            string lockFilePath = Data.Value.LockFilePath;
            DebugLogger.LogFormat("Opening lock file: {0}", lockFilePath);

            if (!string.IsNullOrEmpty(lockFilePath))
            {
                try
                {
                    _lockFileStream = File.Open(lockFilePath, FileMode.Append);
                    DebugLogger.Log("Lock file open success");
                }
                catch
                {
                    throw new MultipleInstancesException("Another instance of Patcher spotted");
                }
            }
            else
            {
                DebugLogger.LogWarning("LockFile is missing");
            }
        }

        private void ThreadLoadPatcherConfiguration()
        {
            try
            {
                DebugLogger.Log("Loading patcher configuration...");

                _state.Value = PatcherState.LoadingPatcherConfiguration;

                // TODO: Use PatcherConfigurationReader
                _configuration = DefaultConfiguration;

                DebugLogger.Log("Patcher configuration loaded.");
            }
            catch (ThreadInterruptedException)
            {
                DebugLogger.Log("Loading patcher configuration interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                DebugLogger.Log("Loading patcher configuration aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                DebugLogger.LogError("Error while loading patcher configuration: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void ThreadWaitForUserDecision(CancellationToken cancellationToken)
        {
            try
            {
                DebugLogger.Log("Waiting for user decision...");

                _state.Value = PatcherState.WaitingForUserDecision;

                bool isInstalled = _app.IsInstalled();

                DebugLogger.LogVariable(isInstalled, "isInstalled");

                _canRepairApp.Value = false; // not implemented
                _canInstallApp.Value = !isInstalled;
                _canCheckForAppUpdates.Value = isInstalled;
                _canStartApp.Value = isInstalled;

                if (_canInstallApp.Value && _configuration.AutomaticallyInstallApp && !_hasAutomaticallyInstalledApp)
                {
                    DebugLogger.Log("Automatically deciding to install app.");
                    _hasAutomaticallyInstalledApp = true;
                    _hasAutomaticallyCheckedForAppUpdate = true;
                    _userDecision = UserDecision.InstallAppAutomatically;
                    return;
                }

                if (_canCheckForAppUpdates.Value && _configuration.AutomaticallyCheckForAppUpdates &&
                    !_hasAutomaticallyCheckedForAppUpdate)
                {
                    DebugLogger.Log("Automatically deciding to check for app updates.");
                    _hasAutomaticallyInstalledApp = true;
                    _hasAutomaticallyCheckedForAppUpdate = true;
                    _userDecision = UserDecision.CheckForAppUpdatesAutomatically;
                    return;
                }

                if (_canStartApp.Value && _configuration.AutomaticallyStartApp && !_hasAutomaticallyStartedApp)
                {
                    DebugLogger.Log("Automatically deciding to start app.");
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

                DebugLogger.Log(string.Format("Waiting for user decision result: {0}.", _userDecision));
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log("Waiting for user decision cancelled.");
            }
            catch (ThreadInterruptedException)
            {
                DebugLogger.Log("Waiting for user decision interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                DebugLogger.Log("Waiting for user decision aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                DebugLogger.LogWarning("Error while waiting for user decision: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void ThreadExecuteUserDecision(CancellationToken cancellationToken)
        {
            bool displayWarningInsteadOfError = false;
            
            try
            {
                _warning.Value = string.Empty;
                
                DebugLogger.Log(string.Format("Executing user decision {0}...", _userDecision));

                switch (_userDecision)
                {
                    case UserDecision.None:
                        break;
                    case UserDecision.RepairApp:
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
                }

                DebugLogger.Log(string.Format("User decision {0} execution done.", _userDecision));
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log(string.Format("User decision {0} execution cancelled.", _userDecision));
            }
            catch (UnauthorizedAccessException e)
            {
                DebugLogger.Log(string.Format("User decision {0} execution issue: permissions failure.",
                    _userDecision));
                DebugLogger.LogException(e);

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
                DebugLogger.LogException(e);
                
                if (displayWarningInsteadOfError)
                {
                    _warning.Value = "Unable to check for updates. Please check your internet connection.";
                }
                else
                {
                    ThreadDisplayError(PatcherError.NoInternetConnection, cancellationToken);
                }
            }
            catch (NotEnoughtDiskSpaceException e)
            {
                DebugLogger.LogException(e);
                ThreadDisplayError(PatcherError.NotEnoughDiskSpace, cancellationToken);
            }
            catch (ThreadInterruptedException)
            {
                DebugLogger.Log(string.Format(
                    "User decision {0} execution interrupted: thread has been interrupted. Rethrowing exception.",
                    _userDecision));
                throw;
            }
            catch (ThreadAbortException)
            {
                DebugLogger.Log(string.Format(
                    "User decision {0} execution aborted: thread has been aborted. Rethrowing exception.",
                    _userDecision));
                throw;
            }
            catch (Exception exception)
            {
                DebugLogger.LogWarning(string.Format(
                    "Error while executing user decision {0}: an exception has occured.", _userDecision));
                DebugLogger.LogException(exception);

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
                DebugLogger.Log(string.Format("Displaying patcher error {0}...", error));

                ErrorDialog.Display(error, cancellationToken);

                DebugLogger.Log(string.Format("Patcher error {0} displayed.", error));
            }
            catch (OperationCanceledException)
            {
                DebugLogger.Log(string.Format("Displaying patcher error {0} cancelled.", _userDecision));
            }
            catch (ThreadInterruptedException)
            {
                DebugLogger.Log(string.Format("Displaying patcher error {0} interrupted: thread has been interrupted. Rethrowing exception.", error));
                throw;
            }
            catch (ThreadAbortException)
            {
                DebugLogger.Log(string.Format("Displaying patcher error {0} aborted: thread has been aborted. Rethrowing exception.", error));
                throw;
            }
            catch (Exception)
            {
                DebugLogger.LogWarning(string.Format("Error while displaying patcher error {0}: an exception has occured. Rethrowing exception.", error));
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

            _appInfo.Value = _app.RemoteMetaData.GetAppInfo(!automatically);
            _remoteVersionId.Value = _app.GetLatestVersionId(!automatically);
            if (_app.IsInstalled())
            {
                _localVersionId.Value = _app.GetInstalledVersionId();
            }

            AppLicense appLicense;
            if (_appInfo.Value.UseKeys)
            {
                appLicense = GetAppLicense();
            }
            else
            {
                appLicense = new AppLicense
                {
                    Secret = null
                };
            }

            _updateAppCancellationTokenSource = new CancellationTokenSource();

            using (cancellationToken.Register(() => _updateAppCancellationTokenSource.Cancel()))
            {
                var appUpdater = new AppUpdater( new AppUpdaterContext( _app, _configuration.AppUpdaterConfiguration, appLicense ) );

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

        private AppLicense GetAppLicense()
        {
            var validateLicenseCommand =
                new UnityLicenseValidator(_app._appDataPath, UI.Dialogs.LicenseDialog.Instance);

            validateLicenseCommand.Validate(_app.AppSecret);

            // ReSharper disable once PossibleInvalidOperationException
            return validateLicenseCommand.AppLicense.Value;
        }

        private bool ThreadTryRestartWithRequestForPermissions()
        {
            DebugLogger.Log("Restarting patcher with request for permissions.");

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

                    DebugLogger.Log("Patcher restarted with request for permissions.");

                    return true;
                }

                DebugLogger.Log(string.Format("Restarting patcher with request for permissions not possible: unsupported platform {0}.", applicationPlatform));

                return false;
            }
            catch (ThreadInterruptedException)
            {
                DebugLogger.Log("Restarting patcher with request for permissions interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                DebugLogger.Log("Restarting patcher with request for permissions aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception exception)
            {
                DebugLogger.LogWarning("Error while restarting patcher with request for permissions: an exception has occured.");
                DebugLogger.LogException(exception);

                return false;
            }
        }
    }
}