using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mono.Unix.Native;
using UnityEngine;

namespace PatchKit.Unity.Patcher
{
    [RequireComponent(typeof(Patcher))]
    public class PatcherController : MonoBehaviour
    {
        [Multiline]
        public string EditorCommandLineArgs;

        public string SecretKey { get; private set; }

        public string ApplicationDataPath { get; private set; }

        private Patcher _patcher;

        public void StartApplicationAndQuit()
        {
            StartApplication();

            UnityEngine.Application.Quit();
        }

        public void StartApplication()
        {
            var directoryInfo = new DirectoryInfo(_patcher.ApplicationDataPath);


            ProcessStartInfo processStartInfo = new ProcessStartInfo();

            if (UnityEngine.Application.platform == RuntimePlatform.OSXPlayer ||
                UnityEngine.Application.platform == RuntimePlatform.OSXEditor)
            {
                var executableApp = directoryInfo.GetDirectories("*.app", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (executableApp != null)
                {
                    try
                    {
                        foreach(var file in executableApp.GetFiles("*", SearchOption.AllDirectories))
                        {
                            Syscall.chmod(file.FullName, FilePermissions.ALLPERMS);
                        }
                    }
                    catch (Exception exception)
                    {
                        UnityEngine.Debug.LogException(exception);
                    }

                    processStartInfo.FileName = "open";
                    processStartInfo.Arguments = string.Format("\"{0}\"", executableApp.FullName);
                }
            }
            else
            {
                var executableFile = directoryInfo.GetFiles("*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (executableFile != null)
                {
                    processStartInfo.FileName = executableFile.FullName;
                }
            }

            processStartInfo.WorkingDirectory = _patcher.ApplicationDataPath;

            Process.Start(processStartInfo);
        }

        public void Retry()
        {
            _patcher.StartPatching();
        }

        public void Quit()
        {
            UnityEngine.Application.Quit();
        }

        protected virtual void Awake()
        {
            _patcher = GetComponent<Patcher>();

            string secretKey;

            if (TryReadArgument("--secret", out secretKey))
            {
                SecretKey = DecodeSecret(secretKey);
            }

            string applicationDataPath;

            if (TryReadArgument("--installdir", out applicationDataPath))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                ApplicationDataPath = Path.Combine(GetBaseDirectory(), applicationDataPath);
            }
        }

        protected virtual void Start()
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(SecretKey))
            {
#endif
                _patcher.SecretKey = SecretKey;
#if UNITY_EDITOR
            }
#endif
            _patcher.ApplicationDataPath = ApplicationDataPath;

            _patcher.StartPatching();
        }

        private string GetBaseDirectory()
        {
            if (UnityEngine.Application.isEditor)
            {
                return UnityEngine.Application.persistentDataPath;
            }

            string path = Path.GetDirectoryName(UnityEngine.Application.dataPath);

            if (UnityEngine.Application.platform == RuntimePlatform.OSXPlayer)
            {
                path = Path.GetDirectoryName(path);
            }

            return path;
        }

        private string[] GetCommandLineArgs()
        {
#if UNITY_EDITOR
            return EditorCommandLineArgs.Split(' ');
#else
            return System.Environment.GetCommandLineArgs();
#endif
        }

        private bool TryReadArgument(string argumentName, out string value)
        {
            var args = GetCommandLineArgs().ToList();

            int index = args.IndexOf(argumentName);

            if (index != -1 && index < args.Count - 1)
            {
                value = args[index + 1];

                return true;
            }

            value = null;

            return false;
        }

        private static string DecodeSecret(string encodedSecret)
        {
            var bytes = Convert.FromBase64String(encodedSecret);

            for (int i = 0; i < bytes.Length; ++i)
            {
                byte b = bytes[i];
                bool lsb = (b & 1) > 0;
                b >>= 1;
                b |= (byte)(lsb ? 128 : 0);
                b = (byte)~b;
                bytes[i] = b;
            }

            var chars = new char[bytes.Length / sizeof(char)];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

#if UNITY_EDITOR
        [ContextMenu("Open Editor Application Directory")]
        public void OpenEditorApplicationDirectory()
        {
            UnityEditor.EditorUtility.OpenWithDefaultApp(UnityEngine.Application.persistentDataPath);
        }
#endif
    }
}
