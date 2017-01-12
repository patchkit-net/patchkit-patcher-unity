using System;
using System.Threading;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppUpdater;
using PatchKit.Unity.Patcher.Status;
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

        private readonly CommandLinePatcherDataReader _commandLinePatcherDataReader = new CommandLinePatcherDataReader();

        private Thread _thread;

        private bool _hasInternetConnection;

        private PatcherState _state = PatcherState.None;

        private PatcherData _data;

        private PatcherConfiguration _configuration;

        private bool _canUpdateApp;

        private bool _canStartApp;

        private ILocalData _localData;

        private IRemoteData _remoteData;

        private bool _hasBeenDestroyed;

        private UserDecision _userDecision = UserDecision.None;

        public event Action<OverallStatus> UpdateAppStatusChanged;

        public event Action<PatcherState> StateChanged;

        public event Action<bool> CanUpdateAppChanged;

        public event Action<bool> CanStartAppChanged;

        public PatcherData DebugData;

        public PatcherConfiguration DefaultConfiguration;

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

        private void Start()
        {
            _thread = new Thread(ThreadFunc);
            _thread.Start();
        }

        private void ThreadFunc()
        {
            LoadPatcherData();

            while (!_hasBeenDestroyed)
            {
                CheckInternetConnection();
                LoadPatcherConfiguration();

                if (_localData != null)
                {
                    _localData.TemporaryData.Dispose();
                }

                // Dispose previous instance
                if (_localData != null)
                {
                    _localData.Dispose();
                }

                _localData = new LocalData(_data.AppDataPath);
                _remoteData = new RemoteData(_data.AppSecret);

                if (_hasInternetConnection)
                {
                    if (_configuration.UpdateAppAutomatically)
                    {
                        if (!UpdateApp())
                        {
                            continue;
                        }
                    }

                    if (_configuration.StartAppAutomatically)
                    {
                        StartApp();
                        return;
                    }

                    do
                    {
                        WaitForUserDecision();

                        if (_userDecision == UserDecision.UpdateApp)
                        {
                            UpdateApp();
                        }
                        else if (_userDecision == UserDecision.StartApp)
                        {
                            StartApp();
                            return;
                        }

                    } while (_userDecision != UserDecision.CheckInternetConnection);
                }

                if (_hasInternetConnection)
                {
                    if (CanUpdateApp && _configuration.UpdateAppAutomatically)
                    {
                        UpdateApp();
                    }

                    if (CanStartApp && _configuration.StartAppAutomatically)
                    {
                        StartApp();
                        return; // Application execution tracking isn't done yet so we exit the application.
                    }
                }




                WaitForUserDecision();

                if (_userDecision == UserDecision.UpdateApp && CanUpdateApp)
                {
                    UpdateApp();
                }
                else if (_userDecision == UserDecision.StartApp && CanStartApp)
                {
                    StartApp();
                    return; // Application execution tracking isn't done yet so we exit the application.
                }
                else if (_userDecision == UserDecision.CheckInternetConnection) // Check internet connection
                {
                    CheckAppStatus();
                }
            }
        }

        private void CheckAppStatus()
        {
            State = PatcherState.CheckingAppStatus;

            

            CanStartApp = _localData.IsInstalled();

            CanUpdateApp = _hasInternetConnection && _localData.IsInstalled() &&
                           _remoteData.MetaData.GetLatestVersionId() > _localData.GetInstalledVersion();
        }

        private void CheckInternetConnection()
        {
            // TODO: Check whether internet connection is available
            _hasInternetConnection = true;
        }

        private void LoadPatcherData()
        {
            if (Application.isEditor)
            {
                _data = DebugData;
            }
            else
            {
                var commandLinePatcherDataReader = new CommandLinePatcherDataReader();
                _data = commandLinePatcherDataReader.Read();
            }
        }

        private void LoadPatcherConfiguration()
        {
            // TODO: Use PatcherConfigurationReader
            _configuration = DefaultConfiguration;
        }

        private bool UpdateApp()
        {
            return true;
        }

        private bool StartApp()
        {
            return true;
        }

        private void DisplayErrorMessage()
        {
            
        }

        private void WaitForUserDecision()
        {
            
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

        private void OnDestroy()
        {
            _hasBeenDestroyed = true;
        }
    }
}