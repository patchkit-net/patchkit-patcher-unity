using System;
using PatchKit.Api;
using PatchKit.Unity.Patcher.AppData.Local;
using PatchKit.Unity.Patcher.AppData.Remote;
using PatchKit.Unity.Patcher.AppUpdater;
using PatchKit.Unity.Patcher.Status;
using PatchKit.Unity.Patcher.UI.Dialogs;
using UnityEngine;

namespace PatchKit.Unity.Patcher
{
    public class PatcherApplication : MonoBehaviour
    {
        private readonly CommandLineParser _commandLineParser = new CommandLineParser();

        private PatcherThread _patcherThread;

        public PatcherApplicationConfiguration Configuration;

        public string DebugAppSecret;

        public event Action<OverallStatus> PatcherOverallStatusChanged;

        public event Action<PatcherApplication> StateChanged;

        public bool IsPatching
        {
            get { return _patcherThread != null && _patcherThread.IsPatching; }
        }

        private void Start()
        {
            if (Configuration.AutomaticalyStartPatching && !IsPatching)
            {
                StartPatching();
            }
        }

        public void StartPatching()
        {
            if (IsPatching)
            {
                throw new InvalidOperationException();
            }

            if (_patcherThread != null)
            {
                _patcherThread.Dispose();
            }

            var patcherData = new AppData.AppData(CreateLocalData(), CreateRemoteData());

            var patcherContext = new AppUpdaterContext(patcherData, Configuration.AppUpdaterConfiguration,
                new StatusMonitor(), FindLicenseDialog());

            var patcher = new AppUpdater.AppUpdater(new AppUpdaterStrategyResolver(), patcherContext);

            _patcherThread = new PatcherThread(patcher);
            _patcherThread.StartPatching();
            _patcherThread.Finished += PatcherThreadOnFinished;
        }

        private void PatcherThreadOnFinished(Exception exception)
        {
            if (exception == null && Configuration.AutomaticalyStartApplication)
            {
                StartApplication();
            }
        }

        public void StartApplication()
        {
            if (IsPatching)
            {
                throw new InvalidOperationException();
            }

            var applicationStarter = new ApplicationStarter(CreateLocalData(), CreateRemoteData());
            applicationStarter.Start();
        }

        private LicenseDialog FindLicenseDialog()
        {
            return FindObjectOfType<LicenseDialog>();
        }

        private IRemoteData CreateRemoteData()
        {
            return new RemoteData(GetAppSecret(),
                CreateMainApiConnection(),
                CreateKeysApiConnection());
        }

        private ILocalData CreateLocalData()
        {
            return new LocalData(GetLocalDataPath());
        }

        private MainApiConnection CreateMainApiConnection()
        {
            return new MainApiConnection(Settings.GetMainApiConnectionSettings());
        }

        private KeysApiConnection CreateKeysApiConnection()
        {
            return new KeysApiConnection(Settings.GetKeysApiConnectionSettings());
        }

        private string GetAppSecret()
        {
            if (Application.isEditor)
            {
                return DebugAppSecret;
            }

            string appSecret;

            if (!_commandLineParser.TryParseAppSecret(out appSecret))
            {
                throw new ApplicationException("Unable to parse app secret from command line.");
            }

            return appSecret;
        }

        private string GetLocalDataPath()
        {
            return "";
        }
    }
}