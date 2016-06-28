using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

            FileInfo executableFile;

            if (UnityEngine.Application.platform == RuntimePlatform.OSXPlayer ||
                UnityEngine.Application.platform == RuntimePlatform.OSXEditor)
            {
                executableFile = directoryInfo.GetFiles("*.app", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (executableFile != null)
                {
                    Process.Start("open -a " + executableFile.FullName);
                }
            }
            else
            {
                executableFile = directoryInfo.GetFiles("*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (executableFile != null)
                {
                    Process.Start(executableFile.FullName);
                }
            }
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
            _patcher.SecretKey = SecretKey;
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

            int index = args.IndexOf(argumentName) + 1;

            if (index < args.Count)
            {
                value = args[index];

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
