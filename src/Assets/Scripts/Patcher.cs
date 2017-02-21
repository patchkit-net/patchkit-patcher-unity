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

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(Patcher));

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

        public PatcherData Data
        {
            get { return _data; }
        }

        public event Action<OverallStatus> UpdateAppStatusChanged;

        public event Action<PatcherState> StateChanged;

        public event Action<PatcherError> ErrorChanged;

        public event Action<bool> CanUpdateAppChanged;

        public event Action<bool> CanStartAppChanged;

        public event Action<bool> CanCheckInternetConnectionChanged;

        public string EditorAppSecret;

        public int EditorOverrideLatestVersionId;

        public PatcherConfiguration DefaultConfiguration;

        private PatcherState _state = PatcherState.None;

        public PatcherState State
        {
            get { return _state; }
            set
            {
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
                _canCheckInternetConnection = value;
                DebugLogger.LogVariable(_canCheckInternetConnection, "_canCheckInternetConnection");
                OnCanCheckInternetConnectionChanged(_canCheckInternetConnection);
            }
        }

        private PatcherError _error;
        private PatcherData _data;

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

        private void Awake()
        {
            DebugLogger.Log("Awake Unity event.");

            LogSystemInfo();
            LogPatcherInfo();

            Instance = this;
            Dispatcher.Initialize();
            Application.runInBackground = true;
            LoadPatcherData();
        }

        private void Start()
        {
            DebugLogger.Log("Start Unity event.");

            DebugLogger.Log("Starting patcher thread.");
            _thread = new Thread(ThreadFunc);
            _thread.Start();
        }

        private void OnDestroy()
        {
            DebugLogger.Log("OnDestroy Unity event.");

            if(_app != null)
            {
                _app.Dispose();
            }

            DebugLogger.Log("Cleaning up thread.");

            _hasBeenDestroyed = true;

            if (_thread != null && _thread.IsAlive)
            {
                while(_thread.IsAlive)
                {
                    Cancel();

                    DebugLogger.Log("Interrupting thread.");

                    _thread.Interrupt();
                    
                    _thread.Join(1000);

                    if (_thread.IsAlive)
                    {
                        while (_thread.IsAlive)
                        {
                            DebugLogger.Log("Aborting thread.");

                            _thread.Abort();
                            _thread.Join(1000);
                        }
                    }
                }
            }
        }

        private void ThreadFunc()
        {
            try
            {
                CheckInternetConnection();
                LoadPatcherConfiguration();

                while (!_hasBeenDestroyed)
                {
                    try
                    {
                        WaitForUserDecision();

                        DebugLogger.Log(string.Format("Executing user decision - {0}", _userDecision));

                        if (CanCheckInternetConnection && _userDecision == UserDecision.CheckInternetConnection)
                        {
                            CheckInternetConnection();
                            LoadPatcherConfiguration();
                        }
                        else if (CanStartApp && _userDecision == UserDecision.StartApp)
                        {
                            StartApp();
                            Quit();
                        }
                        else if (CanUpdateApp && _userDecision == UserDecision.UpdateApp)
                        {
                            UpdateApp();
                        }
                    }
                    catch (OperationCanceledException)
                    {
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
                DebugLogger.Log("Using Unity Editor patcher data.");
                _data = new PatcherData
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
                _data = commandLinePatcherDataReader.Read();
            }

            DebugLogger.LogVariable(Data.AppSecret, "Data.AppSecret");
            DebugLogger.LogVariable(Data.AppDataPath, "Data.AppDataPath");
            DebugLogger.LogVariable(Data.OverrideLatestVersionId, "Data.OverrideLatestVersionId");

            if (_app != null)
            {
                _app.Dispose();
            }

            _app = new App(Data.AppDataPath, Data.AppSecret, Data.OverrideLatestVersionId);
        }

        private void CheckInternetConnection()
        {
            DebugLogger.Log("Checking internet connection.");

            State = PatcherState.CheckingInternetConnection;

            Ping ping = null;

            try
            {
                Dispatcher.Invoke(() => ping = new Ping("8.8.8.8")).WaitOne();

                var watch = new Stopwatch();
                watch.Reset();
                watch.Start();

                bool isDone = false;

                do
                {
                    Dispatcher.Invoke(() => isDone = ping.isDone);

                    // 15 seconds timeout
                    if (watch.ElapsedMilliseconds > 15000)
                    {
                        throw new TimeoutException("Ping has timed out.");
                    }
                } while (!isDone);

                _hasInternetConnection = true;
            }
            catch (Exception exception)
            {
                DebugLogger.LogException(exception);
                _hasInternetConnection = false;
            }
            finally
            {
                if (ping != null)
                {
                    Dispatcher.Invoke(() => ping.DestroyPing());
                }
            }
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

            appUpdater.Update(_cancellationTokenSource.Token);
        }

        private void StartApp()
        {
            DebugLogger.Log("Starting app.");

            State = PatcherState.StartingApp;

            var appStarter = new AppStarter(_app);

            appStarter.Start();
        }

        private void WaitForUserDecision()
        {
            DebugLogger.Log("Waiting for user decision.");

            bool isInstalled = _app.IsInstalled();

            int? installedVersionId = isInstalled ? (int?)_app.GetInstalledVersionId() : null;
            int? latestVersionId = _hasInternetConnection ? (int?)_app.GetLatestVersionId() : null;

            DebugLogger.LogVariable(isInstalled, "isInstalled");
            DebugLogger.LogVariable(_hasInternetConnection, "_hasInternetConnection");
            DebugLogger.LogVariable(installedVersionId, "installedVersionId");
            DebugLogger.LogVariable(latestVersionId, "latestVersionId");

            State = PatcherState.WaitingForUserDecision;

            CanStartApp = isInstalled;

            CanUpdateApp = _hasInternetConnection && (!isInstalled || latestVersionId != installedVersionId.Value);

            CanCheckInternetConnection = !_hasInternetConnection;

            if (CanUpdateApp && _configuration.UpdateAppAutomatically && !_triedToAutomaticallyUpdateApp)
            {
                DebugLogger.Log("Updating app automatically.");
                _triedToAutomaticallyUpdateApp = true;
                _userDecision = UserDecision.UpdateApp;
                return;
            }

            if (CanStartApp && _configuration.StartAppAutomatically && !_triedToAutomaticallyStartApp)
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

            State = PatcherState.HandlingErrorMessage;

            Error = new PatcherError
            {
                Exception = exception
            };

            _errorMessageHandled.Reset();
            _errorMessageHandled.WaitOne();
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