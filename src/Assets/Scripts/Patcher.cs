using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using PatchKit.Unity.Patcher.AppUpdater;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Utilities;
using PatchKit.Unity.Patcher.Debug;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher
{
    public class Patcher : MonoBehaviour
    {
        public enum UserDecision
        {
            None,
            CheckInternetConnection,
            UpdateApp,
            StartApp
        }

        #region Private fields

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(Patcher));

        public static Patcher Instance { get; private set; }

        private Thread _thread;

        private bool _hasInternetConnection;

        private PatcherConfiguration _configuration;

        private App _app;

        private bool _keepThreadAlive;

        private bool _isThreadBeingDestroyed;

        private UserDecision _userDecision = UserDecision.None;

        private readonly ManualResetEvent _userDecisionSetEvent = new ManualResetEvent(false);

        private readonly ManualResetEvent _errorMessageHandled = new ManualResetEvent(false);

        private bool _triedToAutomaticallyUpdateApp;

        private bool _triedToAutomaticallyStartApp;

        private CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region Events

        public event Action<OverallStatus> UpdateAppStatusChanged;

        #endregion

        #region Public fields

        public string EditorAppSecret;

        public int EditorOverrideLatestVersionId;

        public PatcherConfiguration DefaultConfiguration;

        #endregion

        #region Public properties

        private readonly BoolReactiveProperty _canUpdateApp = new BoolReactiveProperty(false);

        public IReadOnlyReactiveProperty<bool> CanUpdateApp
        {
            get { return _canUpdateApp; }
        }

        private readonly BoolReactiveProperty _canStartApp = new BoolReactiveProperty(false);

        public IReadOnlyReactiveProperty<bool> CanStartApp
        {
            get { return _canStartApp; }
        }

        private readonly BoolReactiveProperty _canCheckInternetConnection = new BoolReactiveProperty(false);

        public IReadOnlyReactiveProperty<bool> CanCheckInternetConnection
        {
            get { return _canCheckInternetConnection; }
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

        private readonly ReactiveProperty<PatcherError> _error = new ReactiveProperty<PatcherError>();

        public IReadOnlyReactiveProperty<PatcherError> Error
        {
            get { return _error; }
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

        public void SetErrorMessageHandled()
        {
            DebugLogger.Log("Setting error message as handled.");

            _errorMessageHandled.Set();
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

        #region Logging operations

        private void LogSystemInfo()
        {
            DebugLogger.LogVariable(Environment.Version, "Environment.Version");
            DebugLogger.LogVariable(Environment.OSVersion, "Environment.OSVersion");
        }

        private string GetPatcherVersion()
        {
            string versionFilePath = Path.Combine(Application.streamingAssetsPath, "patcher.versioninfo");

            if (File.Exists(versionFilePath))
            {
                return File.ReadAllText(versionFilePath);
            }

            return "unknown";
        }

        private void LogPatcherInfo()
        {
            DebugLogger.Log(string.Format("Patcher version - {0}", GetPatcherVersion()));
        }

        #endregion

        #region Unity events

        private void Awake()
        {
            DebugLogger.Log("Awake Unity event.");

            Instance = this;
            Dispatcher.Initialize();
            Application.runInBackground = true;

            LogSystemInfo();
            LogPatcherInfo();

            try
            {
                LoadPatcherData();
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception);
                DebugLogger.LogError("Unable to load patcher data. Terminating application.");

                Application.Quit();
            }
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
                CheckInternetConnection();
                LoadPatcherConfiguration();

                while (_keepThreadAlive)
                {
                    try
                    {
                        WaitForUserDecision();

                        DebugLogger.Log(string.Format("Executing user decision - {0}", _userDecision));

                        if (_canCheckInternetConnection.Value && _userDecision == UserDecision.CheckInternetConnection)
                        {
                            CheckInternetConnection();
                            LoadPatcherConfiguration();
                        }
                        else if (_canStartApp.Value && _userDecision == UserDecision.StartApp)
                        {
                            StartApp();
                            Quit();
                        }
                        else if (_canUpdateApp.Value && _userDecision == UserDecision.UpdateApp)
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
        }

        private void CheckInternetConnection()
        {
            DebugLogger.Log("Checking internet connection.");

            _state.Value = PatcherState.CheckingInternetConnection;

            _hasInternetConnection = true;
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

            int? installedVersionId = isInstalled ? (int?)_app.GetInstalledVersionId() : null;

            DebugLogger.LogVariable(isInstalled, "isInstalled");
            DebugLogger.LogVariable(_hasInternetConnection, "_hasInternetConnection");
            DebugLogger.LogVariable(installedVersionId, "installedVersionId");

            _state.Value = PatcherState.WaitingForUserDecision;

            _canStartApp.Value = isInstalled;

            _canUpdateApp.Value = _hasInternetConnection;

            _canCheckInternetConnection.Value = !_hasInternetConnection;

            if (_canUpdateApp.Value && _configuration.UpdateAppAutomatically && !_triedToAutomaticallyUpdateApp)
            {
                DebugLogger.Log("Updating app automatically.");
                _triedToAutomaticallyUpdateApp = true;
                _userDecision = UserDecision.UpdateApp;
                return;
            }

            if (_canStartApp.Value && _configuration.StartAppAutomatically && !_triedToAutomaticallyStartApp)
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

            _error.Value = new PatcherError
            {
                Exception = exception
            };

            _errorMessageHandled.Reset();
            _errorMessageHandled.WaitOne();
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