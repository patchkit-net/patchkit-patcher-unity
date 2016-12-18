using System;
using System.Diagnostics;
using System.Linq;
using PatchKit.Unity.Patcher.Licensing;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PatchKit.Unity.Patcher
{
    public class PatcherApplication : MonoBehaviour
    {
        public static PatcherApplication Instance { get; private set; }

        public bool StartPatcherOnAwake = true;

        public Patcher Patcher { get; private set; }

        [Header("Configuration is overwritten in standalone build by values from command line arguments.")]
        public PatcherConfiguration Configuration;

        public void StartApplication()
        {
            new PatcherAppStarter(Configuration.ApplicationDataPath).StartApplication();
        }

        public void StartApplicationAndQuit()
        {
            StartApplication();

            Application.Quit();
        }

        public void StartPatching()
        {
            Patcher.Start();
        }

        public void RetryPatching()
        {
            Patcher.Start();
        }

        public void CancelPatching()
        {
            Patcher.Cancel();
        }

        public void Quit()
        {
            Application.Quit();
        }

        protected virtual void Awake()
        {
            Instance = this;
            Application.runInBackground = true;

            PrepareConfiguration();
            PreparePatcher();

            if (StartPatcherOnAwake)
            {
                Patcher.Start();
            }
        }

        protected virtual void OnApplicationQuit()
        {
            ((IDisposable)Patcher).Dispose();
        }

        private void PrepareConfiguration()
        {
            if (Application.isEditor)
            {
                Configuration.ApplicationDataPath = Application.dataPath.Replace("/Assets", string.Format("/Temp/PatcherApp{0}", Configuration.AppSecret));
            }
            else
            {
                var configurationParser = new PatcherConfigurationParser();
                configurationParser.Parse(ref Configuration);
            }
        }

        private void PreparePatcher()
        {
            Patcher = new Patcher(Configuration, FindObjectOfType<KeyLicenseObtainer>());

            Patcher.OnStateChanged += state =>
            {
                if (state == PatcherState.UnauthorizedAccess)
                {
                    RestartWithAdminPrivilages();
                }
            };
        }

        private static void RestartWithAdminPrivilages()
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer)
            {
                Debug.Log("Trying to restart patcher application with administrator privilages.");

                var info = new ProcessStartInfo
                {
                    FileName = Application.dataPath.Replace("_Data", ".exe"),
                    Arguments =
                        string.Join(" ", Environment.GetCommandLineArgs().Select(s => "\"" + s + "\"").ToArray()),
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(info);

                Application.Quit();
            }
        }
    }
}
