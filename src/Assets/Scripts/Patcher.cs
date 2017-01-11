using System;
using System.Threading;
using PatchKit.Api;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppUpdater;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Patcher.UI.Dialogs;
using UnityEngine;

namespace PatchKit.Unity.Patcher
{
    public class Patcher : MonoBehaviour
    {
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
            while (!_hasBeenDestroyed)
            {
                Prepare();

                if (_hasInternetConnection)
                {
                    if (_configuration.UpdateAppAutomatically)
                    {

                    }
                    else
                    {
                        if (_configuration.StartAppAutomatically)
                        {

                        }
                        else
                        {

                        }
                    }
                }
            }
        }

        private void Prepare()
        {
            State = PatcherState.Preparing;

            CheckInternetConnection();
            LoadPatcherData();
            LoadPatcherConfiguration();
            
            // Logic for assumption that internet connection is available

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

        private void UpdateApp()
        {

        }

        private void PatcherThreadOnFinished(Exception exception)
        {
            if (exception == null && Configuration.AutomaticalyStartApplication)
            {
                StartApplication();
            }
        }

        public void StartApp()
        {
            if (IsPatching)
            {
                throw new InvalidOperationException();
            }

            var applicationStarter = new AppStarter(CreateLocalData(), CreateRemoteData());
            applicationStarter.Start();
        }

        private LicenseDialog FindLicenseDialog()
        {
            return FindObjectOfType<LicenseDialog>();
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
            _hasInternetConnection = true;
        }
    }
}