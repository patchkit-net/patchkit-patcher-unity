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

        private Rect _popupRect, _rect, _texturePopupRect;
        private bool _show;
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
            _texturePopupRect = new Rect(0, popupRectY - y, windowWidth, 120);
            _graphicRaycaster = FindObjectOfType<GraphicRaycaster>();
        }

        void OnGUI()
        {
            Event ec = Event.current;
            if (ec.type == EventType.KeyDown && ec.keyCode == KeyCode.D && ec.control && ec.shift)
            {
                if (_show)
                {
                    Close();
                }
                else
                {
                    Open();
                }
            }

            if (_show)
            {
                GUI.DrawTexture(_rect, Texture2D.whiteTexture);
                GUI.Window(0, _rect, Draw, "Debug Menu");
                if (_showPopup)
                {
                    GUI.Window(1, _popupRect, DrawPopup, "Information");
                }
            }
        }

        void Draw(int id)
        {
            if (_showPopup)
            {
                GUI.enabled = false;
                GUI.FocusControl(null);
                GUI.FocusWindow(1);
                GUI.BringWindowToFront(1);
            }

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

            if (_showPopup)
            {
                GUI.DrawTexture(_texturePopupRect, Texture2D.whiteTexture);
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
            try
            {
                Process.Start(path);
                Close();
            }
            catch (Exception e)
            {
                OpenPopup(string.Format("The directory/file cannot be found: {0}", path));
            }
        }

        private void OpenPopup(string popupMessage)
        {
            _popupMessage = popupMessage;
            DebugLogger.LogError(popupMessage);
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
                Close();
            }
        }

        void Close()
        {
            _show = false;
            _showPopup = false;
            _graphicRaycaster.enabled = true;
        }

        void Open()
        {
            _show = true;
            _graphicRaycaster.enabled = false;
        }
    }
}
