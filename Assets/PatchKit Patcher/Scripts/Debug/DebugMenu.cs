using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using PatchKit.Unity.Utilities;
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
        private PlatformType _platformType;

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
            _platformType = Platform.GetPlatformType();
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

            if (Patcher.Instance.IsAppInstalled.Value &&
                Patcher.Instance.State.Value == PatcherState.WaitingForUserDecision)
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

        void OpenFile(string path, bool isFile)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                var processStartInfo = GetProcessStartInfo(path);
                StartProcess(processStartInfo, isFile);
                Close();
            }
            else
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
#if UNITY_STANDALONE_OSX
            string logDirectoryPath = Patcher.Instance.Data.Value.LockFilePath.Replace(
                Patcher.Instance.AppSecret + Path.DirectorySeparatorChar + ".lock", "");
#else
            string logDirectoryPath = Patcher.Instance.Data.Value.LockFilePath.Replace(".lock","");
#endif
            OpenFile(logDirectoryPath, false);
#endif
        }

        private void OpenLauncherLogFile()
        {
#if UNITY_EDITOR
            OpenPopup("Access to Launcher in the editor is not possible");
#else
#if UNITY_STANDALONE_OSX
            string logPath = Patcher.Instance.Data.Value.LockFilePath.Replace(
                Patcher.Instance.AppSecret + Path.DirectorySeparatorChar + ".lock", "launcher-log.txt");
#else
            string logPath = Patcher.Instance.Data.Value.LockFilePath.Replace(".lock","launcher-log.txt");
#endif
            OpenFile(logPath, true);
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
            var logDirectoryPath = string.Format("{0}/.config/unity3d/{1}/{2}",
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                Application.companyName,
                Application.productName);
#elif UNITY_STANDALONE_OSX
    #if UNITY_2019_1_OR_NEWER
            var logDirectoryPath = string.Format("{1}/Library/Logs/{2}/{3}",
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                Application.companyName,
                Application.productName);
    #else
            var logDirectoryPath = string.Format("{0}/Library/Logs/Unity",
                Environment.GetFolderPath(Environment.SpecialFolder.Personal));
    #endif
#endif
#endif
            OpenFile(logDirectoryPath, false);
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
            var logPath = string.Format("{0}/.config/unity3d/{1}/{2}/Player.log",
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                Application.companyName,
                Application.productName);
#elif UNITY_STANDALONE_OSX
#if UNITY_2019_1_OR_NEWER
            var logPath = string.Format("{0}/Library/Logs/{0}/{1}/Player.log",
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                Application.companyName,
                Application.productName);
#else
            var logPath = string.Format("{0}/Library/Logs/Unity/Player.log",
                Environment.GetFolderPath(Environment.SpecialFolder.Personal));
#endif
#endif
#endif
            OpenFile(logPath, true);
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

        private ProcessStartInfo GetProcessStartInfo(string executablePath)
        {
            string workingDir = Path.GetDirectoryName(executablePath) ?? string.Empty;
            switch (_platformType)
            {
                case PlatformType.Unknown:
                    throw new ArgumentException("Unknown");
                case PlatformType.Windows:
                    return new ProcessStartInfo
                    {
                        FileName = executablePath,
                        WorkingDirectory = workingDir
                    };
                case PlatformType.OSX:

                    return new ProcessStartInfo
                    {
                        FileName = "open",
                        Arguments = string.Format("\"{0}", executablePath),
                        WorkingDirectory = workingDir
                    };
                case PlatformType.Linux:
                    return new ProcessStartInfo
                    {
                        FileName = executablePath,
                        WorkingDirectory = workingDir
                    };
                default:
                    throw new ArgumentOutOfRangeException("platform", _platformType, null);
            }
        }

        private void StartProcess(ProcessStartInfo processStartInfo, bool isFile)
        {
            DebugLogger.Log(string.Format("Starting process '{0}'", processStartInfo.FileName));

            var process = Process.Start(processStartInfo);
            if (process == null)
            {
                DebugLogger.LogError(string.Format("Failed to start process {0}", processStartInfo.FileName));
            }
            else if (isFile && process.HasExited)
            {
                DebugLogger.LogError(string.Format("Process '{0}' prematurely exited with code '{1}'",
                    processStartInfo.FileName, process.ExitCode));
            }
        }
    }
}