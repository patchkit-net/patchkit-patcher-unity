using System;
using System.Diagnostics;
using PatchKit.Unity.Patcher;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit_Patcher.Scripts.Debug
{
    public class DebugMenu : MonoBehaviour
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(DebugMenu));

        private Rect _popUp, _debugMenu;
        private bool _show;
        private bool _showPopUp;
        private string _message;
        private GraphicRaycaster _graphicRaycaster;

        void Start()
        {
            int windowWidth = 250;
            int windowHeight = 180;
            int x = (Screen.width - windowWidth) / 2;
            int y = (Screen.height - windowHeight) / 2;
            int yPopUp = (Screen.height - 120) / 2;
            _debugMenu = new Rect(x, y, windowWidth, windowHeight);
            _popUp = new Rect(x, yPopUp, windowWidth, 120);
            _graphicRaycaster = FindObjectOfType<GraphicRaycaster>();
        }

        void OnGUI()
        {
            if (Event.current.Equals(Event.KeyboardEvent(KeyCode.D.ToString())))
            {
                _show = !_show;
                _graphicRaycaster.enabled = !_graphicRaycaster.enabled;
            }

            if (_show)
            {
                GUI.DrawTexture(_debugMenu, Texture2D.whiteTexture);
                GUI.Window(0, _debugMenu, Menu, "Debug Menu");
            }
            else if (_showPopUp)
            {
                GUI.DrawTexture(_popUp, Texture2D.whiteTexture);
                GUI.Window(1, _popUp, PopUp, "Information");
            }
        }

        void Menu(int i)
        {
            if (GUILayout.Button("Open Patcher log file"))
            {
                OpenPatcherLogFile();
            }

            if (GUILayout.Button("Open Patcher log file location"))
            {
                OpenPatcherLogFileLocation();
            }

            if (GUILayout.Button("Open Launcher log file"))
            {
                OpenLauncherLogFile();
            }

            if (GUILayout.Button("Open Launcher log file location"))
            {
                OpenLauncherLogFileLocation();
            }

            if (Patcher.Instance.IsAppInstalled.Value)
            {
                if (GUILayout.Button("Verify all app files"))
                {
                    VerifyAllAppFiles();
                }

                if (GUILayout.Button("Remove all app files"))
                {
                    RemoveAllAppFiles();
                }
            }
        }

        private void VerifyAllAppFiles()
        {
            SetUserDecision(Patcher.UserDecision.VerifyFiles);
        }

        private void RemoveAllAppFiles()
        {
            SetUserDecision(Patcher.UserDecision.UninstallApp);
        }

        private void SetUserDecision(Patcher.UserDecision userDecision)
        {
            while (Patcher.Instance.State.Value != PatcherState.WaitingForUserDecision)
            {
            }

            Patcher.Instance.SetUserDecision(userDecision);

            _show = false;
            _graphicRaycaster.enabled = true;
        }

        void Open(string path)
        {
            _show = false;
            try
            {
                Process.Start(path);
                _graphicRaycaster.enabled = true;
            }
            catch (Exception e)
            {
                _message = string.Format("The directory/file cannot be found: {0}", path);
                DebugLogger.LogError(_message);
                _showPopUp = true;
            }
        }

        private void OpenLauncherLogFileLocation()
        {
#if UNITY_EDITOR
            _message = "Access to Launcher in the editor is not possible";
            _show = false;
            _showPopUp = true;
#else
            string logDirectoryPath = Patcher.Instance.Data.Value.LockFilePath.Replace(".lock","");
            Open(logDirectoryPath);
#endif
        }

        private void OpenLauncherLogFile()
        {
#if UNITY_EDITOR
            _message = "Access to Launcher in the editor is not possible";
            _show = false;
            _showPopUp = true;
#else
            string logPath = Patcher.Instance.Data.Value.LockFilePath.Replace(".lock","launcher-log.txt");
            Open(logPath);
#endif
        }

        private void OpenPatcherLogFileLocation()
        {
#if UNITY_EDITOR
            var logDirectoryPath = Application.consoleLogPath.Replace("Editor.log", "");
#else
#if UNITY_STANDALONE_WIN
                var logDirectoryPath = string.Format("{0}",
                    Application.persistentDataPath);
#elif UNITY_STANDALONE_LINUX
                var logDirectoryPath = string.Format("~/.config/unity3d/{0}/{1}",
                    Application.companyName,
                    Application.productName);
#elif UNITY_STANDALONE_OSX
    #if UNITY_2019_1_OR_NEWER
                var logDirectoryPath = string.Format("~/Library/Logs/{0}/{1}",
                    Application.companyName,
                    Application.productName);
    #else
                var logDirectoryPath = "~/Library/Logs/Unity";
    #endif
#endif
#endif
            Open(logDirectoryPath);
        }

        private void OpenPatcherLogFile()
        {
#if UNITY_EDITOR
            var logPath = Application.consoleLogPath;
#else
#if UNITY_STANDALONE_WIN
#if UNITY_2019_1_OR_NEWER
                var logPath = string.Format("{0}/Player.log",
                    Application.persistentDataPath);
#else
                    var logPath = string.Format("{0}/output_log.txt",
                    Application.persistentDataPath);
#endif
#elif UNITY_STANDALONE_LINUX
                var logPath = string.Format("~/.config/unity3d/{0}/{1}/Player.log",
                    Application.companyName,
                    Application.productName);
#elif UNITY_STANDALONE_OSX
#if UNITY_2019_1_OR_NEWER
                var logPath = string.Format("~/Library/Logs/{0}/{1}/Player.log",
                    Application.companyName,
                    Application.productName);
               
#else
                var logPath = "~/Library/Logs/Unity/Player.log";
#endif
#endif
#endif
            Open(logPath);
        }

        void PopUp(int i)
        {
            GUILayout.Label(_message);
            if (GUILayout.Button("OK"))
            {
                _showPopUp = false;
                _graphicRaycaster.enabled = true;
            }
        }
    }
}
