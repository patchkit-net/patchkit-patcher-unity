using System;
using System.Collections;
using System.Diagnostics;
using PatchKit.Unity.Patcher;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.Debug
{
    public class DebugMenu : MonoBehaviour
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(DebugMenu));

        private Rect _popupRect, _rect;
        private bool _showDebugMenu;
        private bool _showPopup;
        private string _popupMessage;
        private GraphicRaycaster _graphicRaycaster;

        void Start()
        {
            int windowWidth = 250;
            int windowHeight = 180;
            int x = (Screen.width - windowWidth) / 2;
            int y = (Screen.height - windowHeight) / 2;
            int popupRectY = (Screen.height - 120) / 2;
            _rect = new Rect(x, y, windowWidth, windowHeight);
            _popupRect = new Rect(x, popupRectY, windowWidth, 120);
            _graphicRaycaster = FindObjectOfType<GraphicRaycaster>();
        }

        void OnGUI()
        {
            if (Event.current.Equals(Event.KeyboardEvent(KeyCode.D.ToString())))
            {
                if (_showDebugMenu)
                {
                    Close();
                }
                else
                {
                    Open();
                }
            }

            if (_showDebugMenu)
            {
                GUI.DrawTexture(_rect, Texture2D.whiteTexture);
                GUI.Window(0, _rect, Draw, "Debug Menu");
            }
            else if (_showPopup)
            {
                GUI.DrawTexture(_popupRect, Texture2D.whiteTexture);
                GUI.Window(1, _popupRect, DrawPopup, "Information");
            }
        }

        void Draw(int id)
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
            StartCoroutine(SetUserDecision(Patcher.UserDecision.VerifyFiles));
        }

        private void RemoveAllAppFiles()
        {
            StartCoroutine(SetUserDecision(Patcher.UserDecision.UninstallApp));
        }

        private IEnumerator SetUserDecision(Patcher.UserDecision userDecision)
        {
            while (Patcher.Instance.State.Value != PatcherState.WaitingForUserDecision)
            {
                yield return new WaitForSeconds(0.5f);
            }

            Patcher.Instance.SetUserDecision(userDecision);

            Close();
        }

        void OpenFile(string path)
        {
            Close();
            try
            {
                Process.Start(path);
            }
            catch (Exception e)
            {
                _graphicRaycaster.enabled = false;
                OpenPopup(string.Format("The directory/file cannot be found: {0}", path));
            }
        }

        private void OpenPopup(string popupMessage)
        {
            _popupMessage = popupMessage;
            DebugLogger.LogError(popupMessage);
            _showDebugMenu = false;
            _showPopup = true;
        }

        private void OpenLauncherLogFileLocation()
        {
#if UNITY_EDITOR
            OpenPopup("Access to Launcher in the editor is not possible");
#else
            string logDirectoryPath = Patcher.Instance.Data.Value.LockFilePath.Replace(".lock","");
            Open(logDirectoryPath);
#endif
        }

        private void OpenLauncherLogFile()
        {
#if UNITY_EDITOR
            OpenPopup("Access to Launcher in the editor is not possible");
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
            OpenFile(logDirectoryPath);
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
            OpenFile(logPath);
        }

        void DrawPopup(int id)
        {
            GUILayout.Label(_popupMessage);
            if (GUILayout.Button("OK"))
            {
                ClosePopup();
            }
        }

        void Close()
        {
            _showDebugMenu = false;
            _graphicRaycaster.enabled = true;
        }

        void ClosePopup()
        {
            _showPopup = false;
            _graphicRaycaster.enabled = true;
        }

        void Open()
        {
            _showDebugMenu = true;
            _graphicRaycaster.enabled = false;
        }
    }
}
