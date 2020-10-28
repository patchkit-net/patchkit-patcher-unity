using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using PatchKit.Unity.Patcher;
using PatchKit.Unity.Patcher.AppData.FileSystem;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine;

public class DebugMenu : MonoBehaviour
{
    private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(App));
    
    private Rect _popUp, _debugMenu;
    private bool _show = false;
    private bool _showPopUp = false;
    private string _message;

    void Start () {
        int windowWidth = 250;
        int windowHeight = 180;
        int x = (Screen.width - windowWidth) / 2;
        int y = (Screen.height - windowHeight) / 2;
        int yPopUp = (Screen.height - 120) / 2;
        _debugMenu = new Rect(x,y,windowWidth,windowHeight);
        _popUp = new Rect(x,yPopUp,windowWidth,120);
    }

    void OnGUI ()
    {
        if (Event.current.Equals(Event.KeyboardEvent(KeyCode.D.ToString())))
            _show = !_show;
        if (_show)
        {
            GUI.Window(0, _debugMenu, Menu, "Debug Menu");
        }
        else if(_showPopUp)
        {
            GUI.Window(1,_popUp, PopUp, "Information");
        }
    }
        void Menu(int i) {
            if(GUILayout.Button("Open Patcher log file")) {
                OpenPatcherLogFile();
            }
            
            if(GUILayout.Button("Open Patcher log file location")){
                OpenPatcherLogFileLocation();
            }
            
            if(GUILayout.Button("Open Launcher log file")){
                OpenLauncherLogFile();
            }

            if (GUILayout.Button("Open Launcher log file location")){
                OpenLauncherLogFileLocation();
            }
            
            if (GUILayout.Button("Verify all app files")){
                VerifyAllAppFiles();
            }
            
            if (GUILayout.Button("Remove all app files")){
                RemoveAllAppFiles();
            }
        }

        private void VerifyAllAppFiles()
        {
            Patcher.Instance.CancelUpdateApp();
            Thread thread = new Thread(() => {
                while (Patcher.Instance.State.Value != PatcherState.WaitingForUserDecision) { }

                Patcher.Instance.SetUserDecision(Patcher.UserDecision.InstallApp);})
            {
                IsBackground = true
            };
            thread.Start();

            _message = "Verifying all application files";
            _show = false;
            _showPopUp = true;
        }

        private void RemoveAllAppFiles()
        {
            string appDataPath = Patcher.Instance.Data.Value.AppDataPath;
            Patcher.Instance.CancelUpdateApp();
            try
            {
                Directory.Delete(appDataPath, true);
                DebugLogger.Log("Application directory deleted.");
                _message = "Application directory has been removed";
                _show = false;
                _showPopUp = true;
            }
            catch (Exception e)
            {
                _message = string.Format("The directory cannot be found: {0}", appDataPath);
                DebugLogger.LogError(_message);
                _show = false;
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
            _show = false;
            try
            {
                Process.Start(logDirectoryPath);
            }
            catch (Exception e)
            {
                _message = string.Format("The directory cannot be found: {0}", logDirectoryPath);
                DebugLogger.LogError(_message);
                _showPopUp = true;
            }
        }
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
            _show = false;
            try
            {
                Process.Start(logPath);
            }
            catch (Exception e)
            {
                _message = string.Format("The file cannot be found: {0}", logPath);
                DebugLogger.LogError(_message);
                _showPopUp = true;
            }
#endif
        }

        private void OpenPatcherLogFileLocation()
        {
#if UNITY_EDITOR
            var logDirectoryPath = Application.consoleLogPath.Replace("Editor.log","");
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
            _show = false;
            try
            {
                Process.Start(logDirectoryPath);
            }
            catch (Exception e)
            {
                _message = string.Format("The directory cannot be found: {0}", logDirectoryPath);
                DebugLogger.LogError(_message);
                _showPopUp = true;
            }
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
            _show = false;
            try
            {
                Process.Start(logPath);
            }
            catch (Exception e)
            {
                _message = string.Format("The file cannot be found: {0}", logPath);
                DebugLogger.LogError(_message);
                _showPopUp = true;
            }
        }

        void PopUp(int i)
        {
            GUI.Label(
                new Rect(10, 20, 230, 88),
                _message);

            if (GUI.Button(new Rect(62, 90, 125, 20), "OK"))
            {
                _showPopUp = false;
            }
        }
}
