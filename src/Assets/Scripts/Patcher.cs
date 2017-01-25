using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using PatchKit.Unity.Patcher.AppUpdater;
using PatchKit.Unity.Patcher.Cancellation;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Utilities;
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
            StartApp,
            Quit
        }

        private Thread _thread;

        private bool _hasInternetConnection;

        private PatcherData _data;

        private PatcherConfiguration _configuration;

        private App _app;

        private bool _hasBeenDestroyed;

        private UserDecision _userDecision = UserDecision.None;

        private readonly ManualResetEvent _userDecisionSetEvent = new ManualResetEvent(false);

        private bool _triedToAutomaticallyUpdateApp;

        private bool _triedToAutomaticallyStartApp;

        private CancellationTokenSource _cancellationTokenSource;

        public event Action<OverallStatus> UpdateAppStatusChanged;

        public event Action<PatcherState> StateChanged;

        public event Action<bool> CanUpdateAppChanged;

        public event Action<bool> CanStartAppChanged;

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
                OnCanStartAppChanged(_canStartApp);
            }
        }

        public void SetUserDecision(UserDecision userDecision)
        {
            _userDecision = userDecision;
            _userDecisionSetEvent.Set();
        }

        public void Cancel()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        private void Awake()
        {
            Dispatcher.Initialize();
            Application.runInBackground = true;
        }

        private void Start()
        {
            LoadPatcherData();

            _thread = new Thread(ThreadFunc);
            _thread.Start();
        }

        private void OnDestroy()
        {
            if (_thread != null && _thread.IsAlive)
            {
                _thread.Abort();
            }
            _hasBeenDestroyed = true;
        }

        private void ThreadFunc()
        {
            CheckInternetConnection();
            LoadPatcherConfiguration();

            while (!_hasBeenDestroyed)
            {
                WaitForUserDecision();

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
                    catch (Exception exception)
                    {
                        DisplayErrorMessage(exception);
                    }
                }
                else if (_userDecision == UserDecision.UpdateApp)
                {
                    try
                    {
                        UpdateApp();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        RestartWithRequestForPermissions();
                        Quit();
                    }
                    catch (Exception exception)
                    {
                        DisplayErrorMessage(exception);
                    }
                }
                else if (_userDecision == UserDecision.Quit)
                {
                    Quit();
                }
            }
        }

        private void LoadPatcherData()
        {
            if (Application.isEditor)
            {
                _data.AppSecret = DebugAppSecret;
                _data.AppDataPath = Application.dataPath.Replace("/Assets",
                    string.Format("/Temp/PatcherApp{0}", DebugAppSecret));
            }
            else
            {
                var commandLinePatcherDataReader = new CommandLinePatcherDataReader();
                _data = commandLinePatcherDataReader.Read();
            }

            if (_app != null)
            {
                _app.Dispose();
            }

            _app = new App(_data.AppDataPath, _data.AppSecret);
        }

        private void CheckInternetConnection()
        {
            State = PatcherState.CheckingInternetConnection;

            // TODO: Check whether internet connection is available
            _hasInternetConnection = true;
        }

        private void LoadPatcherConfiguration()
        {
            State = PatcherState.LoadingPatcherConfiguration;

            // TODO: Use PatcherConfigurationReader
            _configuration = DefaultConfiguration;
        }

        private void UpdateApp()
        {
            State = PatcherState.UpdatingApp;

            _cancellationTokenSource = new CancellationTokenSource();

            var appUpdater = new AppUpdater.AppUpdater(_app, _configuration.AppUpdaterConfiguration);

            appUpdater.Context.StatusMonitor.OverallStatusChanged += OnUpdateAppStatusChanged;

            appUpdater.Patch(_cancellationTokenSource.Token);
        }

        private void StartApp()
        {
            State = PatcherState.StartingApp;

            var appStarter = new AppStarter(_app);

            appStarter.Start();
        }

        private void RestartWithRequestForPermissions()
        {
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

        private void DisplayErrorMessage(Exception exception)
        {
            State = PatcherState.DisplayingErrorMessage;

            UnityEngine.Debug.LogError(exception);
        }

        private void WaitForUserDecision()
        {
            State = PatcherState.WaitingForUserDecision;

            CanStartApp = _app.IsInstalled();

            CanUpdateApp = _hasInternetConnection && (!_app.IsInstalled() ||
                           _app.RemoteMetaData.GetLatestVersionId() > _app.GetInstalledVersionId());

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

        private void Quit()
        {
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

        protected virtual void OnUpdateAppStatusChanged(OverallStatus obj)
        {
            if (UpdateAppStatusChanged != null) UpdateAppStatusChanged(obj);
        }

        protected virtual void OnStateChanged(PatcherState obj)
        {
            if (StateChanged != null) StateChanged(obj);
        }

        protected virtual void OnCanUpdateAppChanged(bool obj)
        {
            if (CanUpdateAppChanged != null) CanUpdateAppChanged(obj);
        }

        protected virtual void OnCanStartAppChanged(bool obj)
        {
            if (CanStartAppChanged != null) CanStartAppChanged(obj);
        }
    }
}