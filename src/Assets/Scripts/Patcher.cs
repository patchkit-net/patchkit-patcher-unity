using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using PatchKit.Unity.Patcher.AppUpdater;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Utilities;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Patcher.UI.Dialogs;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher
{
    public class Patcher : MonoBehaviour
    {
        public enum UserDecision
        {
            None,
            RepairApp,
            StartApp,
            InstallApp,
            CheckForAppUpdates
        }

        #region Private fields

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(Patcher));

        public static Patcher Instance { get; private set; }

        private Thread _thread;

        private PatcherConfiguration _configuration;

        private App _app;

        private bool _keepThreadAlive;

        private bool _isThreadBeingDestroyed;

        private UserDecision _userDecision = UserDecision.None;

        private readonly ManualResetEvent _userDecisionSetEvent = new ManualResetEvent(false);

        private bool _triedToAutomaticallyInstallApp;

        private bool _triedToAutomaticallyCheckForAppUpdates;

        private bool _triedToAutomaticallyStartApp;

        private CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region Events

        public event Action<OverallStatus> UpdateAppStatusChanged;

        #endregion

        #region Public fields

        public ErrorDialog ErrorDialog;

        public string EditorAppSecret;

        public int EditorOverrideLatestVersionId;

        public PatcherConfiguration DefaultConfiguration;

        #endregion

        #region Public properties

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

        #endregion

        #region Public methods

        public void SetUserDecision(UserDecision userDecision)
        {
            DebugLogger.Log("Setting user deicision.");

            _userDecision = userDecision;
            DebugLogger.LogVariable(_userDecision, "_userDecision");
            _userDecisionSetEvent.Set();
        }

        public void Quit()
        {
            DebugLogger.Log("Qutting.");
            
            Dispatcher.Invoke(() =>
            {
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
            }).WaitOne();
        }

        public void Cancel()
        {
            DebugLogger.Log("Cancelling.");

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        #endregion

        #region Unity events

        private void Awake()
        {
            try
            {
                Instance = this;
                Dispatcher.Initialize();

                DebugLogger.Log(string.Format("patchkit-patcher-unity: {0}", PatcherInfo.GetVersion()));
                DebugLogger.Log(string.Format("System version: {0}", EnvironmentInfo.GetSystemVersion()));
                DebugLogger.Log(string.Format("Runtime version: {0}", EnvironmentInfo.GetSystemVersion()));

                DebugLogger.Log("Initializing patcher...");
            
                
                Application.runInBackground = true;
            }
            catch (Exception exception)
            {
                DebugLogger.Log("Error while initializing patcher: an exception has occured.");
                DebugLogger.LogException(exception);
                DebugLogger.LogError("Unable to load patcher data. Terminating application.");

                Application.Quit();
            }
            
            DebugLogger.Log("Patcher initialized.");
        }

        private void Start()
        {
            DebugLogger.Log("Start Unity event.");

            DebugLogger.Log("Starting patcher thread.");

            _keepThreadAlive = true;
            _thread = new Thread(ThreadFunc);
            _thread.Start();
        }

        private void OnApplicationQuit()
        {
            DebugLogger.Log("OnApplicationQuit Unity event.");

            if (_thread != null && _thread.IsAlive)
            {
                DebugLogger.Log("Cancelling quit because patcher thread is still alive.");

                Application.CancelQuit();
                StartCoroutine(DestroyPatcherThreadAndQuit());
            }
        }

        private void OnDestroy()
        {
            DebugLogger.Log("OnDestroy Unity event.");

            if (_thread != null && _thread.IsAlive)
            {
                DestroyPatcherThread();
            }
        }

        private void DestroyPatcherThread()
        {
            DebugLogger.Log("Destroying patcher thread.");

            while (_thread != null && _thread.IsAlive)
            {
                DebugLogger.Log("Trying to safely destroy patcher thread.");

                _keepThreadAlive = false;
                Cancel();
                _thread.Interrupt();

                _thread.Join(1000);

                if (_thread.IsAlive)
                {
                    DebugLogger.Log("Trying to force destroy patcher thread.");

                    _thread.Abort();
                    _thread.Join(1000);
                }
            }

            DebugLogger.Log("Patcher thread has been destroyed.");
        }

        private IEnumerator DestroyPatcherThreadAndQuit()
        {
            DebugLogger.Log("Destroying patcher thread.");

            if (_isThreadBeingDestroyed)
            {
                DebugLogger.Log("Patcher thread is already being destroyed.");
                yield break;
            }

            _isThreadBeingDestroyed = true;

            while (_thread != null && _thread.IsAlive)
            {
                DebugLogger.Log("Trying to safely destroy patcher thread.");

                _keepThreadAlive = false;
                Cancel();
                _thread.Interrupt();

                float startTime = Time.unscaledTime;
                while (Time.unscaledTime - startTime < 1.0f && _thread.IsAlive)
                {
                    yield return null;
                }

                if (_thread.IsAlive)
                {
                    DebugLogger.Log("Trying to force destroy patcher thread.");

                    _thread.Abort();
                    startTime = Time.unscaledTime;
                    while (Time.unscaledTime - startTime < 1.0f && _thread.IsAlive)
                    {
                        yield return null;
                    }
                }
            }

            DebugLogger.Log("Patcher thread has been destroyed. Quitting application.");
            Application.Quit();
        }

        #endregion

        #region Thread

        private void ThreadFunc()
        {
            try
            {
                LoadPatcherData();

                LoadPatcherConfiguration();

                while (_keepThreadAlive)
                {
                    try
                    {
                        WaitForUserDecision();

                        DebugLogger.Log(string.Format("Executing user decision - {0}", _userDecision));

                        if (_canStartApp.Value && _userDecision == UserDecision.StartApp)
                        {
                            StartApp();
                            Quit();
                        }
                        else if (_canInstallApp.Value && _userDecision == UserDecision.InstallApp)
                        {
                            UpdateApp();
                        }
                        else if (_canCheckForAppUpdates.Value && _userDecision == UserDecision.CheckForAppUpdates)
                        {
                            UpdateApp();
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        DebugLogger.Log("Patcher has been cancelled.");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        throw;
                    }
                    catch (ThreadInterruptedException)
                    {
                        throw;
                    }
                    catch (ThreadAbortException)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        DebugLogger.LogException(exception);
                        HandleErrorMessage(exception);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                RestartWithRequestForPermissions();
                Quit();
            }
            catch (ThreadInterruptedException)
            {
                DebugLogger.Log("Thread has been interrupted.");
            }
            catch (ThreadAbortException)
            {
                DebugLogger.Log("Thread has been aborted.");
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception);
                DebugLogger.LogError("Exception in patcher thread.");
                Quit();
            }
            finally
            {
                if (_app != null)
                {
                    _app.Dispose();
                }

                DebugLogger.Log("Patcher thread has been destroyed.");
            }
        }

        private void LoadPatcherData()
        {
            DebugLogger.Log("Loading patcher data.");

            Dispatcher.Invoke(() =>
            {
                if (Application.isEditor)
                {
                    DebugLogger.Log("Using Unity Editor patcher data.");
                    _data.Value = new PatcherData
                    {
                        AppSecret = EditorAppSecret,
                        AppDataPath = Application.dataPath.Replace("/Assets",
                            string.Format("/Temp/PatcherApp{0}", EditorAppSecret)),
                        OverrideLatestVersionId = EditorOverrideLatestVersionId
                    };
                }
                else
                {
                    DebugLogger.Log("Loading patcher data from command line.");
                    var commandLinePatcherDataReader = new CommandLinePatcherDataReader();
                    _data.Value = commandLinePatcherDataReader.Read();
                }

                DebugLogger.LogVariable(_data.Value.AppSecret, "Data.AppSecret");
                DebugLogger.LogVariable(_data.Value.AppDataPath, "Data.AppDataPath");
                DebugLogger.LogVariable(_data.Value.OverrideLatestVersionId, "Data.OverrideLatestVersionId");

                if (_app != null)
                {
                    _app.Dispose();
                }

                _app = new App(_data.Value.AppDataPath, _data.Value.AppSecret, _data.Value.OverrideLatestVersionId);
            }).WaitOne();
        }

        private void LoadPatcherConfiguration()
        {
            DebugLogger.Log("Loading patcher configuration.");

            _state.Value = PatcherState.LoadingPatcherConfiguration;

            // TODO: Use PatcherConfigurationReader
            _configuration = DefaultConfiguration;
        }

        private void UpdateApp()
        {
            DebugLogger.Log("Updating app.");

            _state.Value = PatcherState.UpdatingApp;

            _cancellationTokenSource = new CancellationTokenSource();

            var appUpdater = new AppUpdater.AppUpdater(_app, _configuration.AppUpdaterConfiguration);

            appUpdater.Context.StatusMonitor.OverallStatusChanged += OnUpdateAppStatusChanged;

            appUpdater.Update(_cancellationTokenSource.Token);
        }

        private void StartApp()
        {
            DebugLogger.Log("Starting app.");

            _state.Value = PatcherState.StartingApp;

            var appStarter = new AppStarter(_app);

            appStarter.Start();
        }

        private void WaitForUserDecision()
        {
            DebugLogger.Log("Waiting for user decision.");

            bool isInstalled = _app.IsInstalled();

            DebugLogger.LogVariable(isInstalled, "isInstalled");

            _state.Value = PatcherState.WaitingForUserDecision;

            _canRepairApp.Value = false; // not implemented
            _canInstallApp.Value = !isInstalled;
            _canCheckForAppUpdates.Value = isInstalled;
            _canStartApp.Value = isInstalled;

            if (_canInstallApp.Value && _configuration.AutomaticallyInstallApp && !_triedToAutomaticallyInstallApp)
            {
                DebugLogger.Log("Installing app automatically.");
                _triedToAutomaticallyInstallApp = true;
                _userDecision = UserDecision.InstallApp;
                return;
            }

            if (_canCheckForAppUpdates.Value && _configuration.AutomaticallyCheckForAppUpdates && !_triedToAutomaticallyCheckForAppUpdates)
            {
                DebugLogger.Log("Checking for app updates automatically.");
                _triedToAutomaticallyCheckForAppUpdates = true;
                _userDecision = UserDecision.CheckForAppUpdates;
                return;
            }

            if (_canStartApp.Value && _configuration.AutomaticallyStartApp && !_triedToAutomaticallyStartApp)
            {
                DebugLogger.Log("Starting app automatically.");
                _triedToAutomaticallyStartApp = true;
                _userDecision = UserDecision.StartApp;
                return;
            }

            _userDecisionSetEvent.Reset();
            _userDecisionSetEvent.WaitOne();
        }

        private void RestartWithRequestForPermissions()
        {
            DebugLogger.Log("Restarting patcher with request for permissions.");

            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                var info = new ProcessStartInfo
                {
                    FileName = Application.dataPath.Replace("_Data", ".exe"),
                    Arguments =
                        string.Join(" ", Environment.GetCommandLineArgs().Select(s => "\"" + s + "\"").ToArray()),
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(info);
            }
        }

        private void HandleErrorMessage(Exception exception)
        {
            DebugLogger.Log("Handling error message.");

            _state.Value = PatcherState.HandlingErrorMessage;

            ErrorDialog.Display(PatcherError.Other);
        }

        #endregion

        protected virtual void OnUpdateAppStatusChanged(OverallStatus obj)
        {
            Dispatcher.Invoke(() =>
            {
                if (UpdateAppStatusChanged != null) UpdateAppStatusChanged(obj);
            });
        }
    }
}