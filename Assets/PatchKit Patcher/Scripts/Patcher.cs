using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using PatchKit.Api;
using PatchKit.Unity.Patcher.AppUpdater;
using PatchKit.Unity.Utilities;
using PatchKit.Unity.Patcher.Debugging;
using PatchKit.Unity.Patcher.UI.Dialogs;
using UniRx;
using UnityEngine;
using System.IO;
using PatchKit.Apps;
using PatchKit.Apps.Updating;
using PatchKit.Core.IO;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit_Patcher.Scripts;
using UnityEngine.Assertions;
using CancellationToken = System.Threading.CancellationToken;
using Debug = UnityEngine.Debug;

namespace PatchKit.Unity.Patcher
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

        private bool _isForceQuitting;

        private PatcherConfiguration _configuration;

        private UserDecision _userDecision = UserDecision.None;

        private readonly ManualResetEvent _userDecisionSetEvent = new ManualResetEvent(false);

        private bool _hasAutomaticallyInstalledApp;

        private bool _hasAutomaticallyCheckedForAppUpdate;

        private bool _hasAutomaticallyStartedApp;

        private bool _wasUpdateSuccessfulOrNotNecessary = false;
        private bool _hasGameBeenStarted = false;

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

        private readonly BoolReactiveProperty _isAppInstalled = new BoolReactiveProperty(false);

        public IReadOnlyReactiveProperty<bool> IsAppInstalled
        {
            get { return _isAppInstalled; }
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

        private readonly ReactiveProperty<PatcherStateKindOld> _state = new ReactiveProperty<PatcherStateKindOld>(PatcherStateKindOld.None);

        public IReadOnlyReactiveProperty<PatcherStateKindOld> State
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
            Debug.Log($"User deicision set to {userDecision}.");

            _userDecision = userDecision;
            _userDecisionSetEvent.Set();
        }

        public void CancelUpdateApp()
        {
            if (_updateAppCancellationTokenSource != null)
            {
                Debug.Log("Cancelling update app execution.");

                _updateAppCancellationTokenSource.Cancel();
            }
        }

        public void Quit()
        {
            Debug.Log("Quitting application.");

#if UNITY_EDITOR
            if (Application.isEditor)
            {
                UnityEditor.EditorApplication.isPlaying = false;
            }
            else
#endif
            {
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

                    Debug.Log("Deleting the lock file.");
                    if (File.Exists(_data.Value.LockFilePath))
                    {
                        File.Delete(_data.Value.LockFilePath);
                    }
                }
            }
            catch(Exception e)
            {
                Debug.LogWarning("Lock file closing error - " + e);
            }
        }

        private void Awake()
        {
            bool is64Bit = IntPtr.Size == 8;

            if (Application.platform == RuntimePlatform.LinuxEditor ||
                Application.platform == RuntimePlatform.LinuxPlayer)
            {
                LibPatchKitApps.SetPlatformType(is64Bit
                    ? LibPatchKitAppsPlatformType.Linux64
                    : LibPatchKitAppsPlatformType.Linux32);
            }

            if (Application.platform == RuntimePlatform.WindowsEditor ||
                Application.platform == RuntimePlatform.WindowsPlayer)
            {
                LibPatchKitApps.SetPlatformType(is64Bit
                    ? LibPatchKitAppsPlatformType.Win32
                    : LibPatchKitAppsPlatformType.Win64);
            }

            if (Application.platform == RuntimePlatform.OSXEditor ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                LibPatchKitApps.SetPlatformType(LibPatchKitAppsPlatformType.Osx64);
            }

            Assert.raiseExceptions = true;

            Assert.IsNull(_instance, "There must be only one instance of Patcher component.");
            Assert.IsNotNull(ErrorDialog, "ErrorDialog must be set.");

            _instance = this;
            UnityDispatcher.Initialize();
            Application.runInBackground = true;

            Debug.LogFormat("patchkit-patcher-unity: {0}", Version.Text);
            Debug.LogFormat("System version: {0}", EnvironmentInfo.GetSystemVersion());
            Debug.LogFormat("Runtime version: {0}", EnvironmentInfo.GetSystemVersion());

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
                    Debug.LogError("Security issue: EditorAppSecret is set to not allowed value. " +
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
                Debug.Log("Quitting application because patcher thread is not alive.");
                Quit();
            }
        }

        private void OnApplicationQuit()
        {
            Application.CancelQuit();
            StartCoroutine(ForceQuit());
        }

        private IEnumerator ForceQuit()
        {
            if (_isForceQuitting)
            {
                yield break;
            }

            _isForceQuitting = true;

            try
            {
                _canStartThread = false;

                CloseLockFile();

                yield return StartCoroutine(KillThread());

                if (_wasUpdateSuccessfulOrNotNecessary && !_hasGameBeenStarted)
                {
                    yield return StartCoroutine(PatcherStatistics.SendEvent(PatcherStatistics.Event.PatcherSucceededClosed));
                }

                if (!Application.isEditor)
                {
                    Process.GetCurrentProcess().Kill();
                }
            }
            finally
            {
                _isForceQuitting = false;
            }
        }

        private IEnumerator KillThread()
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

            _thread = null;
        }

        private void StartThread()
        {
            Debug.Log("Starting patcher thread...");

            _thread = new Thread(() => ThreadExecution(_threadCancellationTokenSource.Token));
            _thread.Start();
        }

        private void CancelThread()
        {
            Debug.Log("Cancelling patcher thread...");

            _threadCancellationTokenSource.Cancel();
        }

        private void InterruptThread()
        {
            Debug.Log("Interrupting patcher thread...");

            _thread.Interrupt();
        }

        private void AbortThread()
        {
            Debug.Log("Aborting patcher thread...");

            _thread.Abort();
        }

        private void ThreadExecution(CancellationToken cancellationToken)
        {
            try
            {
                _state.Value = PatcherStateKindOld.None;

                Debug.Log("Patcher thread started.");

                try
                {
                    ThreadLoadPatcherData();
                }
                catch (NonLauncherExecutionException)
                {
                    try
                    {
                        LauncherUtilities.ExecuteLauncher();
                        return;
                    }
                    catch (ApplicationException)
                    {
                        ThreadDisplayError(PatcherError.NonLauncherExecution, cancellationToken);
                        return;
                    }
                    finally
                    {
                        Quit();
                    }
                }

                EnsureSingleInstance();

                ThreadLoadPatcherConfiguration();

                PatcherStatistics.TryDispatchSendEvent(PatcherStatistics.Event.PatcherStarted);

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
                Debug.Log("Patcher thread finished: thread has been cancelled.");
            }
            catch (ThreadInterruptedException)
            {
                Debug.Log("Patcher thread finished: thread has been interrupted.");
            }
            catch (ThreadAbortException)
            {
                Debug.Log("Patcher thread finished: thread has been aborted.");
            }
            catch (MultipleInstancesException exception)
            {
                Debug.LogException(exception);
                Quit();
            }
            catch (Exception exception)
            {
                Debug.LogError("Patcher thread failed: an exception has occured.");
                Debug.LogException(exception);
            }
            finally
            {
                _state.Value = PatcherStateKindOld.None;
            }
        }

        private void ThreadLoadPatcherData()
        {
            try
            {
                Debug.Log("Loading patcher data...");
                _state.Value = PatcherStateKindOld.LoadingPatcherData;

#if UNITY_EDITOR
                UnityDispatcher.Invoke(() =>
                {
                    Debug.Log("Using Unity Editor patcher data.");
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
                Debug.Log($"Data.AppSecret = {_data.Value.AppSecret}");
                Debug.Log($"Data.AppDataPath = {_data.Value.AppDataPath}");
                Debug.Log($"Data.OverrideLatestVersionId = {_data.Value.OverrideLatestVersionId}");
                Debug.Log($"Data.LockFilePath = {_data.Value.LockFilePath}");

                Debug.Log("Patcher data loaded.");
            }
            catch (ThreadInterruptedException)
            {
                Debug.Log("Loading patcher data interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                Debug.Log("Loading patcher data aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                Debug.LogError("Error while loading patcher data: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void EnsureSingleInstance()
        {
            string lockFilePath = Data.Value.LockFilePath;
            Debug.LogFormat("Opening lock file: {0}", lockFilePath);

            if (!string.IsNullOrEmpty(lockFilePath))
            {
                try
                {
                    _lockFileStream = File.Open(lockFilePath, FileMode.Append);
                    Debug.Log("Lock file open success");
                }
                catch
                {
                    throw new MultipleInstancesException("Another instance of Patcher spotted");
                }
            }
            else
            {
                Debug.LogWarning("LockFile is missing");
            }
        }

        private void ThreadLoadPatcherConfiguration()
        {
            try
            {
                Debug.Log("Loading patcher configuration...");

                _state.Value = PatcherStateKindOld.LoadingPatcherConfiguration;

                // TODO: Use PatcherConfigurationReader
                _configuration = DefaultConfiguration;

                Debug.Log("Patcher configuration loaded.");
            }
            catch (ThreadInterruptedException)
            {
                Debug.Log("Loading patcher configuration interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                Debug.Log("Loading patcher configuration aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                Debug.LogError("Error while loading patcher configuration: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private bool CheckIfAppIsInstalled()
        {
            return LibPkAppsContainer.Resolve<IsAppInstalledDelegate>()(
                new App(new Core.IO.Path(_data.Value.AppDataPath)), null);
        }

        private void ThreadWaitForUserDecision(CancellationToken cancellationToken)
        {
            try
            {
                Debug.Log("Waiting for user decision...");

                _state.Value = PatcherStateKindOld.WaitingForUserDecision;

                bool isInstalled = CheckIfAppIsInstalled();

                Debug.Log($"isInstalled = {isInstalled}");

                bool canRepairApp = false; // not implemented
                bool canInstallApp = !isInstalled;
                bool canCheckForAppUpdates = isInstalled;
                bool canStartApp = isInstalled;

                _isAppInstalled.Value = isInstalled;

                _canRepairApp.Value = false;
                _canInstallApp.Value = false;
                _canCheckForAppUpdates.Value = false;
                _canStartApp.Value = false;

                if (canInstallApp && _configuration.AutomaticallyInstallApp && !_hasAutomaticallyInstalledApp)
                {
                    Debug.Log("Automatically deciding to install app.");
                    _hasAutomaticallyInstalledApp = true;
                    _hasAutomaticallyCheckedForAppUpdate = true;
                    _userDecision = UserDecision.InstallAppAutomatically;
                    return;
                }

                if (canCheckForAppUpdates && _configuration.AutomaticallyCheckForAppUpdates &&
                    !_hasAutomaticallyCheckedForAppUpdate)
                {
                    Debug.Log("Automatically deciding to check for app updates.");
                    _hasAutomaticallyInstalledApp = true;
                    _hasAutomaticallyCheckedForAppUpdate = true;
                    _userDecision = UserDecision.CheckForAppUpdatesAutomatically;
                    return;
                }

                if (canStartApp && _configuration.AutomaticallyStartApp && !_hasAutomaticallyStartedApp)
                {
                    Debug.Log("Automatically deciding to start app.");
                    _hasAutomaticallyStartedApp = true;
                    _userDecision = UserDecision.StartAppAutomatically;
                    return;
                }

                _canRepairApp.Value = canRepairApp;
                _canInstallApp.Value = canInstallApp;
                _canCheckForAppUpdates.Value = canCheckForAppUpdates;
                _canStartApp.Value = canStartApp;

                _userDecisionSetEvent.Reset();
                using (cancellationToken.Register(() => _userDecisionSetEvent.Set()))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _userDecisionSetEvent.WaitOne();
                }

                _canRepairApp.Value = false;
                _canInstallApp.Value = false;
                _canCheckForAppUpdates.Value = false;
                _canStartApp.Value = false;

                cancellationToken.ThrowIfCancellationRequested();

                Debug.Log($"Waiting for user decision result: {_userDecision}.");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Waiting for user decision cancelled.");
            }
            catch (ThreadInterruptedException)
            {
                Debug.Log("Waiting for user decision interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                Debug.Log("Waiting for user decision aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                Debug.LogWarning("Error while waiting for user decision: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void ThreadExecuteUserDecision(CancellationToken cancellationToken)
        {
            bool displayWarningInsteadOfError = false;

            try
            {
                _warning.Value = string.Empty;

                Debug.Log($"Executing user decision {_userDecision}...");

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
                        displayWarningInsteadOfError = CheckIfAppIsInstalled();
                        ThreadUpdateApp(true, cancellationToken);
                        break;
                    case UserDecision.InstallApp:
                        ThreadUpdateApp(false, cancellationToken);
                        break;
                    case UserDecision.CheckForAppUpdatesAutomatically:
                        displayWarningInsteadOfError = CheckIfAppIsInstalled();
                        ThreadUpdateApp(true, cancellationToken);
                        break;
                    case UserDecision.CheckForAppUpdates:
                        ThreadUpdateApp(false, cancellationToken);
                        break;
                }

                Debug.Log($"User decision {_userDecision} execution done.");
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"User decision {_userDecision} execution cancelled.");
            }
            catch (UnauthorizedAccess e)
            {
                Debug.Log($"User decision {_userDecision} execution issue: permissions failure.");
                Debug.LogException(e);

                if (ThreadTryRestartWithRequestForPermissions())
                {
                    UnityDispatcher.Invoke(Quit);
                }
                else
                {
                    ThreadDisplayError(PatcherError.NoPermissions, cancellationToken);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Debug.Log($"User decision {_userDecision} execution issue: permissions failure.");
                Debug.LogException(e);

                if (ThreadTryRestartWithRequestForPermissions())
                {
                    UnityDispatcher.Invoke(Quit);
                }
                else
                {
                    ThreadDisplayError(PatcherError.NoPermissions, cancellationToken);
                }
            }
            catch (OutOfFreeDiskSpace e)
            {
                Debug.LogException(e);
                ThreadDisplayError(PatcherError.NotEnoughDiskSpace, cancellationToken);
            }
            catch (ThreadInterruptedException)
            {
                Debug.Log(
                    $"User decision {_userDecision} execution interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                Debug.Log(
                    $"User decision {_userDecision} execution aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Error while executing user decision {_userDecision}: an exception has occured.");
                Debug.LogException(exception);

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
            PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.PatcherFailed);
            
            try
            {
                _state.Value = PatcherStateKindOld.DisplayingError;

                Debug.Log($"Displaying patcher error {error}...");

                ErrorDialog.Display(error, cancellationToken);

                Debug.Log($"Patcher error {error} displayed.");
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"Displaying patcher error {_userDecision} cancelled.");
            }
            catch (ThreadInterruptedException)
            {
                Debug.Log(
                    $"Displaying patcher error {error} interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                Debug.Log($"Displaying patcher error {error} aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception)
            {
                Debug.LogWarning(
                    $"Error while displaying patcher error {error}: an exception has occured. Rethrowing exception.");
                throw;
            }
        }

        private void ThreadStartApp()
        {
            _state.Value = PatcherStateKindOld.StartingApp;

            using (var startCtx = LibPatchKitApps.StartApp(_data.Value.AppDataPath))
            {
                while (startCtx.Status == LibPatchKitAppsStartAppStatus.InProgress)
                {
                    Thread.Sleep(100);
                }

                if (startCtx.Status == LibPatchKitAppsStartAppStatus.InternalError)
                {
                    Debug.LogError("Start app internal error");
                    return;
                }

                if (startCtx.Status == LibPatchKitAppsStartAppStatus.UnauthorizedAccess)
                {
                    Debug.LogError("Unauthorized access to start app");
                    return;
                }
            }

            Debug.Log("App started");

            PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.PatcherSucceededGameStarted);
            _hasGameBeenStarted = true;

            UnityDispatcher.Invoke(Quit);
        }

        private void ThreadUpdateApp(bool automatically, CancellationToken cancellationToken)
        {
            _state.Value = PatcherStateKindOld.Connecting;

            try
            {
                _appInfo.Value = LibPkAppsContainer.Resolve<IApiConnection>()
                    .GetApplicationInfo(_data.Value.AppSecret, new Core.Timeout(TimeSpan.FromSeconds(15)),
                        CancellationToken.None);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            try
            {
                _remoteVersionId.Value = LibPkAppsContainer.Resolve<IApiConnection>()
                    .GetAppLatestAppVersionId(_data.Value.AppSecret, new Core.Timeout(TimeSpan.FromSeconds(15)),
                        CancellationToken.None).Id;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            var installedVersionId = LibPkAppsContainer.Resolve<GetAppInstalledVersionIdDelegate>()(
                new App(new Core.IO.Path(_data.Value.AppDataPath)),
                null);

            if (installedVersionId.HasValue)
            {
                _localVersionId.Value = installedVersionId.Value;
            }

            _updateAppCancellationTokenSource = new CancellationTokenSource();

            using (cancellationToken.Register(() => _updateAppCancellationTokenSource.Cancel()))
            {
                try
                {
                    string licenseKey = null;

                    bool retry = true;

                    while (retry)
                    {
                        var updaterStatus = new UpdaterStatus();
                        var downloadStatus = new DownloadStatus {Bytes = {Value = 0}, TotalBytes = {Value = 0}};

                        updaterStatus.RegisterOperation(downloadStatus);
                        downloadStatus.Weight.Value = 1.0;
                        downloadStatus.IsActive.Value = true;

                        _updaterStatus.Value = updaterStatus;
                        _state.Value = PatcherStateKindOld.UpdatingApp;

                        using (var updateCtx = LibPatchKitApps.UpdateAppLatest(
                            _data.Value.AppDataPath,
                            _data.Value.AppSecret,
                            licenseKey))
                        {
                            using (_updateAppCancellationTokenSource.Token.Register(() => updateCtx.Cancel()))
                            {
                                var lastUpdate = DateTime.Now;

                                while (updateCtx.Status == LibPatchKitAppsUpdateAppStatus.InProgress)
                                {
                                    Thread.Sleep(100);

                                    if (DateTime.Now - lastUpdate < TimeSpan.FromSeconds(1))
                                    {
                                        continue;
                                    }

                                    lastUpdate = DateTime.Now;

                                    downloadStatus.Bytes.Value = updateCtx.Progress.InstalledBytes;
                                    downloadStatus.TotalBytes.Value = updateCtx.Progress.TotalBytes;
                                }
                            }

                            Debug.Log("Update result: " + updateCtx.Status);

                            retry = updateCtx.Status != LibPatchKitAppsUpdateAppStatus.Success;

                            if (updateCtx.Status == LibPatchKitAppsUpdateAppStatus.AppLicenseKeyRequired)
                            {
                                var result = LicenseDialog.Instance.Display(LicenseDialogMessageType.None);

                                if (result.Type == LicenseDialogResultType.Aborted)
                                {
                                    throw new OperationCanceledException();
                                }

                                licenseKey = new AppLicenseKey(result.Key);
                            }

                            if (updateCtx.Status == LibPatchKitAppsUpdateAppStatus.InvalidAppLicenseKey)
                            {
                                var result = LicenseDialog.Instance.Display(LicenseDialogMessageType.InvalidLicense);

                                if (result.Type == LicenseDialogResultType.Aborted)
                                {
                                    throw new OperationCanceledException();
                                }

                                licenseKey = new AppLicenseKey(result.Key);
                            }

                            if (updateCtx.Status == LibPatchKitAppsUpdateAppStatus.BlockedAppLicenseKey)
                            {
                                var result = LicenseDialog.Instance.Display(LicenseDialogMessageType.BlockedLicense);

                                if (result.Type == LicenseDialogResultType.Aborted)
                                {
                                    throw new OperationCanceledException();
                                }

                                licenseKey = new AppLicenseKey(result.Key);
                            }

                            if (updateCtx.Status == LibPatchKitAppsUpdateAppStatus.UnauthorizedAccess)
                            {
                                throw new UnauthorizedAccess();
                            }

                            if (updateCtx.Status == LibPatchKitAppsUpdateAppStatus.OutOfFreeDiskSpace)
                            {
                                throw new OutOfFreeDiskSpace();
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    PatcherStatistics.DispatchSendEvent(PatcherStatistics.Event.PatcherCanceled);

                    throw;
                }
                finally
                {
                    _state.Value = PatcherStateKindOld.None;

                    _updaterStatus.Value = null;
                    _updateAppCancellationTokenSource = null;
                }
            }
        }

        private bool ThreadTryRestartWithRequestForPermissions()
        {
            Debug.Log("Restarting patcher with request for permissions.");

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

                    Debug.Log("Patcher restarted with request for permissions.");

                    return true;
                }

                Debug.Log(
                    $"Restarting patcher with request for permissions not possible: unsupported platform {applicationPlatform}.");

                return false;
            }
            catch (ThreadInterruptedException)
            {
                Debug.Log("Restarting patcher with request for permissions interrupted: thread has been interrupted. Rethrowing exception.");
                throw;
            }
            catch (ThreadAbortException)
            {
                Debug.Log("Restarting patcher with request for permissions aborted: thread has been aborted. Rethrowing exception.");
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Error while restarting patcher with request for permissions: an exception has occured.");
                Debug.LogException(exception);

                return false;
            }
        }
    }
}