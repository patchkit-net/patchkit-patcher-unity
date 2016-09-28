using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PatchKit.Unity.Patcher
{
    public class PatcherApplication : MonoBehaviour
    {
        public static PatcherApplication Instance { get; private set; }

        public Patcher Patcher { get; private set; }
        
        [Header("Configuration is overwritten in standalone build by values from command line arguments.")]
        public PatcherConfiguration Configuration;

        public void StartApplicationAndQuit()
        {
            StartApplication();

            Application.Quit();
        }

        public void StartApplication()
        {
            var directoryInfo = new DirectoryInfo(Configuration.ApplicationDataPath);

            ProcessStartInfo processStartInfo = new ProcessStartInfo();

            if (Application.platform == RuntimePlatform.OSXPlayer ||
                Application.platform == RuntimePlatform.OSXEditor)
            {
                var executableApp = directoryInfo.GetDirectories("*.app", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (executableApp != null)
                {
                    try
                    {
                        foreach(var file in executableApp.GetFiles("*", SearchOption.AllDirectories))
                        {
                            Chmod(file.FullName, "+x");
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
            else if (Application.platform == RuntimePlatform.LinuxPlayer)
            {
                var executableFile = directoryInfo.GetFiles("*", SearchOption.TopDirectoryOnly).Where(info =>
                {
                    // Read magic bytes
                    using (FileStream executableFileStream = File.OpenRead(info.FullName))
                    {
                        using (BinaryReader executableBinaryReader = new BinaryReader(executableFileStream))
                        {
                            byte[] magicBytes = executableBinaryReader.ReadBytes(4);

                            if (magicBytes.Length == 4)
                            {
                                if (magicBytes[0] == 127 && // 7F
                                    magicBytes[1] == 69  && // 45 - 'E'
                                    magicBytes[2] == 76  && // 4c - 'L'
                                    magicBytes[3] == 70)    // 46 - 'F'
                                {
                                    return true;
                                }
                            }
                        }
                    }

                    return false;
                }).FirstOrDefault();

                if (executableFile != null)
                {
                    Chmod(executableFile.FullName, "+x");
                    processStartInfo.FileName = executableFile.FullName;
                }
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor ||
                    Application.platform == RuntimePlatform.WindowsPlayer)
            {
                var executableFile = directoryInfo.GetFiles("*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (executableFile != null)
                {
                    processStartInfo.FileName = executableFile.FullName;
                }
            }

            processStartInfo.WorkingDirectory = Configuration.ApplicationDataPath;

            Process.Start(processStartInfo);
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

            string appSecret;

            if (TryReadArgument("--secret", out appSecret))
            {
                Configuration.AppSecret = DecodeSecret(appSecret);
            }

            string applicationDataPath;

            if (TryReadArgument("--installdir", out applicationDataPath))
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                Configuration.ApplicationDataPath = Path.Combine(GetBaseDirectory(), applicationDataPath);
            }

            string forceVersionStr;

            if (TryReadArgument("--forceversion", out forceVersionStr))
            {
                int forceVersion;

                if (int.TryParse(forceVersionStr, out forceVersion))
                {
                    Configuration.ForceVersion = forceVersion;
                }
            }

            Patcher = new Patcher(Configuration);
        }

        protected virtual void Start()
        {
            Patcher.Start();
        }

        protected virtual void OnApplicationQuit()
        {
            if (Patcher.Status.State == PatcherState.Patching)
            {
                // Cancel application quit until patcher finishes it's work
                Application.CancelQuit();
                // Cancel patcher
                Patcher.Cancel();
                // Retry to quit application after patcher finishes
                Patcher.OnPatcherFinished += patcher =>
                {
                    Application.Quit();
                };
            }
        }

        protected virtual void OnDestroy()
        {
            Patcher.Dispose();
        }

        private string GetBaseDirectory()
        {
            if (Application.isEditor)
            {
                return Application.persistentDataPath;
            }

            string path = Path.GetDirectoryName(Application.dataPath);

            if (Application.platform == RuntimePlatform.OSXPlayer)
            {
                path = Path.GetDirectoryName(path);
            }

            return path;
        }

        private static bool TryReadArgument(string argumentName, out string value)
        {
            var args = Environment.GetCommandLineArgs().ToList();

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

        private static void Chmod(string file, string permissions)
        {
            try
            {
                var process = new Process();
                process.StartInfo.FileName = "/bin/chmod";
                process.StartInfo.Arguments = string.Format("{0} \"{1}\"", permissions, file);
                process.Start();
                process.WaitForExit();
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning("Supressed exception caused by chmod call. This message is harmless while launcher is not working on Unix system.");
                UnityEngine.Debug.Log(exception);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Open Editor Application Directory")]
        public void OpenEditorApplicationDirectory()
        {
            UnityEditor.EditorUtility.OpenWithDefaultApp(Application.persistentDataPath);
        }
#endif
    }
}
