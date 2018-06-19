using System;
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
using PatchKit.Core.Collections.Immutable;
using PatchKit.Network;
using PatchKit.Patching.Unity.UI.Dialogs;
using UniRx;
using UnityEngine;
using CancellationToken = System.Threading.CancellationToken;

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

        private DebugLogger _debugLogger;

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

        public CancellationToken ThreadCancellationToken {
            get {
                return _threadCancellationTokenSource.Token;
            }
        }

        private Thread _thread;

        private bool _isThreadBeingKilled;

        private App _app;

        private PatcherConfiguration _configuration;

        private UserDecision _userDecision = UserDecision.None;

        private readonly ManualResetEvent _userDecisionSetEvent = new ManualResetEvent(false);
        
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

        private readonly ReactiveProperty<Api.Models.App> _appInfo = new ReactiveProperty<Api.Models.App>();

        public IReadOnlyReactiveProperty<Api.Models.App> AppInfo
        {
            get { return _appInfo; }
        }

        public void SetUserDecision(UserDecision userDecision)
        {
            _debugLogger.Log(string.Format("User deicision set to {0}.", userDecision));

            _userDecision = userDecision;
            _userDecisionSetEvent.Set();
        }

        public void CancelUpdateApp()
        {
            if (_updateAppCancellationTokenSource != null)
            {
                _debugLogger.Log("Cancelling update app execution.");

                _updateAppCancellationTokenSource.Cancel();
            }
        }

        public void Quit()
        {
            _debugLogger.Log("Quitting application.");
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
                    
                    _debugLogger.Log("Deleting the lock file.");
                    File.Delete(_data.Value.LockFilePath);
                }
            }
            catch
            {
                _debugLogger.LogWarning("Lock file closing error");
            }
        }

        private void Awake()
        {
            _debugLogger = new DebugLogger(typeof(Patcher));

            UnityEngine.Assertions.Assert.raiseExceptions = true;

            Assert.IsNull(_instance, "There must be only one instance of Patcher component.");
            Assert.IsNotNull(ErrorDialog, "ErrorDialog must be set.");

            _instance = this;
            UnityDispatcher.Initialize();
            Application.runInBackground = true;

            _debugLogger.LogFormat("patchkit-patcher-unity: {0}", Version.Value);
            _debugLogger.LogFormat("System version: {0}", EnvironmentInfo.GetSystemVersion());
            _debugLogger.LogFormat("Runtime version: {0}", EnvironmentInfo.GetSystemVersion());

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
                    _debugLogger.LogError("Security issue: EditorAppSecret is set to not allowed value. " +
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
                _debugLogger.Log("Quitting application because patcher thread is not alive.");
                Quit();
            }
        }

        private void OnApplicationQuit()
        {
            if (_thread != null && _thread.IsAlive)
            {
                _debugLogger.Log("Cancelling application quit because patcher thread is alive.");

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

                UnityDispatcher.Invoke(() => _app = new App(_data.Value.AppDataPath, _data.Value.AppSecret, _data.Value.OverrideLatestVersionId)).WaitOne();

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
                                string.Format("/Temp/PatcherApp{0}", EditorAppSecret)),
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
                _debugLogger.Log("Loading patcher data interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                _debugLogger.Log("Loading patcher data aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                _debugLogger.LogError("Error while loading patcher data: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void EnsureSingleInstance()
        {
            string lockFilePath = Data.Value.LockFilePath;
            _debugLogger.LogFormat("Opening lock file: {0}", lockFilePath);

            if (!string.IsNullOrEmpty(lockFilePath))
            {
                try
                {
                    _lockFileStream = File.Open(lockFilePath, FileMode.Append);
                    _debugLogger.Log("Lock file open success");
                }
                catch
                {
                    throw new MultipleInstancesException("Another instance of Patcher spotted");
                }
            }
            else
            {
                _debugLogger.LogWarning("LockFile is missing");
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
                _debugLogger.Log("Loading patcher configuration interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                _debugLogger.Log("Loading patcher configuration aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                _debugLogger.LogError("Error while loading patcher configuration: an exception has occured. Rethrowing exception.");
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

                _debugLogger.LogVariable(isInstalled, "isInstalled");

                _canRepairApp.Value = false; // not implemented
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

                _debugLogger.Log(string.Format("Waiting for user decision result: {0}.", _userDecision));
            }
            catch (OperationCanceledException)
            {
                _debugLogger.Log("Waiting for user decision cancelled.");
            }
            catch (ThreadInterruptedException)
            {
                _debugLogger.Log("Waiting for user decision interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                _debugLogger.Log("Waiting for user decision aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                _debugLogger.LogWarning("Error while waiting for user decision: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void ThreadExecuteUserDecision(CancellationToken cancellationToken)
        {
            bool displayWarningInsteadOfError = false;
            
            try
            {
                _warning.Value = string.Empty;
                
                _debugLogger.Log(string.Format("Executing user decision {0}...", _userDecision));

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

                _debugLogger.Log(string.Format("User decision {0} execution done.", _userDecision));
            }
            catch (OperationCanceledException)
            {
                _debugLogger.Log(string.Format("User decision {0} execution cancelled.", _userDecision));
            }
            catch (UnauthorizedAccessException e)
            {
                _debugLogger.Log(string.Format("User decision {0} execution issue: permissions failure.",
                    _userDecision));
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
            catch (NotEnoughtDiskSpaceException e)
            {
                _debugLogger.LogException(e);
                ThreadDisplayError(PatcherError.NotEnoughDiskSpace, cancellationToken);
            }
            catch (ThreadInterruptedException)
            {
                _debugLogger.Log(string.Format(
                    "User decision {0} execution interrupted: thread has been interrupted. Rethrowing exception.",
                    _userDecision));
                throw;
            }
            catch (ThreadAbortException)
            {
                _debugLogger.Log(string.Format(
                    "User decision {0} execution aborted: thread has been aborted. Rethrowing exception.",
                    _userDecision));
                throw;
            }
            catch (Exception exception)
            {
                _debugLogger.LogWarning(string.Format(
                    "Error while executing user decision {0}: an exception has occured.", _userDecision));
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
                _debugLogger.Log(string.Format("Displaying patcher error {0}...", error));

                ErrorDialog.Display(error, cancellationToken);

                _debugLogger.Log(string.Format("Patcher error {0} displayed.", error));
            }
            catch (OperationCanceledException)
            {
                _debugLogger.Log(string.Format("Displaying patcher error {0} cancelled.", _userDecision));
            }
            catch (ThreadInterruptedException)
            {
                _debugLogger.Log(string.Format("Displaying patcher error {0} interrupted: thread has been interrupted. Rethrowing exception.", error));
                throw;
            }
            catch (ThreadAbortException)
            {
                _debugLogger.Log(string.Format("Displaying patcher error {0} aborted: thread has been aborted. Rethrowing exception.", error));
                throw;
            }
            catch (Exception)
            {
                _debugLogger.LogWarning(string.Format("Error while displaying patcher error {0}: an exception has occured. Rethrowing exception.", error));
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

            AppLicense appLicense;
            if (_appInfo.Value.UseKeys)
            {
                appLicense = GetAppLicense(cancellationToken);
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

        private AppLicense GetAppLicense(CancellationToken cancellationToken)
        {
            var validateLicenseCommand =
                new UnityLicenseValidator(_app._appDataPath, UI.Dialogs.LicenseDialog.Instance);

            validateLicenseCommand.Validate(_app.AppSecret, cancellationToken);

            // ReSharper disable once PossibleInvalidOperationException
            return validateLicenseCommand.AppLicense.Value;
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

                _debugLogger.Log(string.Format("Restarting patcher with request for permissions not possible: unsupported platform {0}.", applicationPlatform));

                return false;
            }
            catch (ThreadInterruptedException)
            {
                _debugLogger.Log("Restarting patcher with request for permissions interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                _debugLogger.Log("Restarting patcher with request for permissions aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception exception)
            {
                _debugLogger.LogWarning("Error while restarting patcher with request for permissions: an exception has occured.");
                _debugLogger.LogException(exception);

                return false;
            }
        }
    }
}