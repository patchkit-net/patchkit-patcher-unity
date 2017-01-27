using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using PatchKit.Unity.Patcher.AppUpdater;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Utilities;
using PatchKit.Unity.Patcher.Debug;
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

        private static DebugLogger DebugLogger = new DebugLogger(typeof(Patcher));

        public static Patcher Instance { get; private set; }

        private Thread _thread;

        private bool _hasInternetConnection;

        private PatcherConfiguration _configuration;        

        private App _app;

        private bool _hasBeenDestroyed;

        private UserDecision _userDecision = UserDecision.None;

        private readonly ManualResetEvent _userDecisionSetEvent = new ManualResetEvent(false);

        private readonly ManualResetEvent _errorMessageHandled = new ManualResetEvent(false);

        private bool _triedToAutomaticallyUpdateApp;

        private bool _triedToAutomaticallyStartApp;

        private CancellationTokenSource _cancellationTokenSource;

        public PatcherData Data { get; private set; }

        public event Action<OverallStatus> UpdateAppStatusChanged;

        public event Action<PatcherState> StateChanged;

        public event Action<PatcherError> ErrorChanged;

        public event Action<bool> CanUpdateAppChanged;

        public event Action<bool> CanStartAppChanged;

        public event Action<bool> CanCheckInternetConnectionChanged;

        public string DebugAppSecret;

        public PatcherConfiguration DefaultConfiguration;

        private PatcherState _state = PatcherState.None;

        public PatcherState State
        {
            get { return _state; }
            set
            {
                if (_state == value) return;

                _state = value;
                OnStateChanged(_state);
            }
        }

        private bool _canUpdateApp;

        public bool CanUpdateApp
        {
            get { return _canUpdateApp; }
            set
            {
                if (_canUpdateApp == value) return;

                _canUpdateApp = value;
                DebugLogger.LogVariable(_canUpdateApp, "_canUpdateApp");
                OnCanUpdateAppChanged(_canUpdateApp);
            }
        }

        private bool _canStartApp;

        public bool CanStartApp
        {
            get { return _canStartApp; }
            set
            {
                if (_canStartApp == value) return;

                _canStartApp = value;
                DebugLogger.LogVariable(_canStartApp, "_canStartApp");
                OnCanStartAppChanged(_canStartApp);
            }
        }

        private bool _canCheckInternetConnection;

        public bool CanCheckInternetConnection
        {
            get { return _canCheckInternetConnection; }
            set
            {
                if (_canCheckInternetConnection == value) return;

                _canCheckInternetConnection = value;
                DebugLogger.LogVariable(_canCheckInternetConnection, "_canCheckInternetConnection");
                OnCanCheckInternetConnectionChanged(_canCheckInternetConnection);
            }
        }

        private PatcherError _error;

        public PatcherError Error
        {
            get { return _error; }
            set
            {
                _error = value;
                OnErrorChanged(_error);
            }
        }

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
                if (Application.isEditor)
                {
                    UnityEditor.EditorApplication.isPlaying = false;
                }
                else
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

        private void Awake()
        {
            DebugLogger.Log("Awake event.");

            Instance = this;
            Dispatcher.Initialize();
            Application.runInBackground = true;
            LoadPatcherData();
        }

        private void Start()
        {
            DebugLogger.Log("Start event.");

            DebugLogger.Log("Starting patcher thread.");
            _thread = new Thread(ThreadFunc);
            _thread.Start();
        }

        private void OnDestroy()
        {
            DebugLogger.Log("OnDestroy event.");

            if(_app != null)
            {
                _app.Dispose();
            }

            DebugLogger.Log("Cleaning up thread.");
            if (_thread != null && _thread.IsAlive)
            {
                while(_thread.IsAlive)
                {
                    DebugLogger.Log("Interrupting thread.");

                    Cancel();

                    _thread.Interrupt();
                    
                    _thread.Join(5000);
                }
            }
            _hasBeenDestroyed = true;
        }

        private void ThreadFunc()
        {
            try
            {
                CheckInternetConnection();
                LoadPatcherConfiguration();

                while (!_hasBeenDestroyed)
                {
                    WaitForUserDecision();

                    DebugLogger.Log(string.Format("Executing user decision - {0}", _userDecision));

                    if (_userDecision == UserDecision.CheckInternetConnection)
                    {
                        CheckInternetConnection();
                        LoadPatcherConfiguration();
                    }
                    else if (_userDecision == UserDecision.StartApp)
                    {
                        try
                        {
                            StartApp();
                            Quit();
                        }
                        catch (OperationCanceledException)
                        {
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
                    else if (_userDecision == UserDecision.UpdateApp)
                    {
                        try
                        {
                            UpdateApp();
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (ThreadInterruptedException)
                        {
                            throw;
                        }
                        catch (ThreadAbortException)
                        {
                            throw;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            RestartWithRequestForPermissions();
                            Quit();
                        }
                        catch (Exception exception)
                        {
                            DebugLogger.LogException(exception);
                            HandleErrorMessage(exception);
                        }
                    }
                }
            }
            catch (ThreadInterruptedException)
            {
                DebugLogger.Log("Thread has been interrupted.");
            }
            catch(ThreadAbortException)
            {
                DebugLogger.Log("Thread has been aborted.");
            }
            catch(Exception exception)
            {
                DebugLogger.LogException(exception);
                DebugLogger.LogError("Exception in patcher thread.");
                Quit();
            }
        }

        private void LoadPatcherData()
        {
            DebugLogger.Log("Loading patcher data.");

            if (Application.isEditor)
            {
                DebugLogger.Log("Using debug patcher data.");
                Data = new PatcherData
                {
                    AppSecret = DebugAppSecret,
                    AppDataPath = Application.dataPath.Replace("/Assets",
                        string.Format("/Temp/PatcherApp{0}", DebugAppSecret))
                };
            }
            else
            {
                DebugLogger.Log("Loading patcher data from command line.");
                var commandLinePatcherDataReader = new CommandLinePatcherDataReader();
                Data = commandLinePatcherDataReader.Read();
            }

            if (_app != null)
            {
                _app.Dispose();
            }

            _app = new App(Data.AppDataPath, Data.AppSecret);
        }

        private void CheckInternetConnection()
        {
            DebugLogger.Log("Checking internet connection.");

            State = PatcherState.CheckingInternetConnection;

            // TODO: Check whether internet connection is available
            _hasInternetConnection = true;
        }

        private void LoadPatcherConfiguration()
        {
            DebugLogger.Log("Loading patcher configuration.");

            State = PatcherState.LoadingPatcherConfiguration;

            // TODO: Use PatcherConfigurationReader
            _configuration = DefaultConfiguration;
        }

        private void UpdateApp()
        {
            DebugLogger.Log("Updating app.");
            
            State = PatcherState.UpdatingApp;

            _cancellationTokenSource = new CancellationTokenSource();

            var appUpdater = new AppUpdater.AppUpdater(_app, _configuration.AppUpdaterConfiguration);

            appUpdater.Context.StatusMonitor.OverallStatusChanged += OnUpdateAppStatusChanged;

            appUpdater.Patch(_cancellationTokenSource.Token);
        }

        private void StartApp()
        {
            DebugLogger.Log("Starting app.");

            State = PatcherState.StartingApp;

            var appStarter = new AppStarter(_app);

            appStarter.Start();
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

            State = PatcherState.HandlingErrorMessage;

            _errorMessageHandled.Reset();
            _errorMessageHandled.WaitOne();
        }

        private void WaitForUserDecision()
        {
            DebugLogger.Log("Waiting for user decision.");

            State = PatcherState.WaitingForUserDecision;

            CanStartApp = _app.IsInstalled();

            CanUpdateApp = _hasInternetConnection && (!_app.IsInstalled() ||
                           _app.RemoteMetaData.GetLatestVersionId() > _app.GetInstalledVersionId());

            CanCheckInternetConnection = !_hasInternetConnection;

            if (CanUpdateApp && _configuration.UpdateAppAutomatically && !_triedToAutomaticallyUpdateApp)
            {
                _triedToAutomaticallyUpdateApp = true;
                _userDecision = UserDecision.UpdateApp;
                return;
            }

            if (CanStartApp && _configuration.StartAppAutomatically && !_triedToAutomaticallyStartApp)
            {
                _triedToAutomaticallyStartApp = true;
                _userDecision = UserDecision.StartApp;
                return;
            }

            _userDecisionSetEvent.Reset();
            _userDecisionSetEvent.WaitOne();
        }

        protected virtual void OnUpdateAppStatusChanged(OverallStatus obj)
        {
            Dispatcher.Invoke(() =>
            {
                if (UpdateAppStatusChanged != null) UpdateAppStatusChanged(obj);
            });
        }

        protected virtual void OnErrorChanged(PatcherError obj)
        {
            Dispatcher.Invoke(() =>
            {
                if (ErrorChanged != null) ErrorChanged(obj);
            });
        }

        protected virtual void OnStateChanged(PatcherState obj)
        {
            Dispatcher.Invoke(() =>
            {
                if (StateChanged != null) StateChanged(obj);
            });
        }

        protected virtual void OnCanUpdateAppChanged(bool obj)
        {
            Dispatcher.Invoke(() =>
            {
                if (CanUpdateAppChanged != null) CanUpdateAppChanged(obj);
            });
        }

        protected virtual void OnCanStartAppChanged(bool obj)
        {
            Dispatcher.Invoke(() =>
            {
                if (CanStartAppChanged != null) CanStartAppChanged(obj);
            });
        }

        protected virtual void OnCanCheckInternetConnectionChanged(bool obj)
        {
            Dispatcher.Invoke(() =>
            {
                if (CanCheckInternetConnectionChanged != null) CanCheckInternetConnectionChanged(obj);
            });
        }
    }
}