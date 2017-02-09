using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PatchKit.Unity.Patcher.AppData;
using PatchKit.Unity.Patcher.Data;
using PatchKit.Unity.Patcher.Debug;
using UnityEngine;

namespace PatchKit.Unity.Patcher
{
    public class AppStarter
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(AppStarter));

        private readonly App _app;

        public AppStarter(App app)
        {
            AssertChecks.ArgumentNotNull(app, "app");

            DebugLogger.LogConstructor();

            _app = app;
        }

        public void Start()
        {
            DebugLogger.Log("Starting.");

            if (Application.platform == RuntimePlatform.OSXPlayer ||
                Application.platform == RuntimePlatform.OSXEditor)
            {
                StartOSXApplication();
            }
            else if (Application.platform == RuntimePlatform.LinuxPlayer)
            {
                StartLinuxApplication();
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor ||
                     Application.platform == RuntimePlatform.WindowsPlayer)
            {
                StartWindowsApplication();
            }
            else
            {
                throw new InvalidOperationException("Unsupported platform.");
            }
        }

        private bool IsInsideRootDirectory(string fileName)
        {
            string dirName = Path.GetDirectoryName(fileName);
            return string.IsNullOrEmpty(dirName);
        }

        private string FindExecutable(Func<string, bool> predicate)
        {
            return _app.LocalMetaData.GetRegisteredEntries().FirstOrDefault(predicate);
        }

        private void StartOSXApplication()
        {
            DebugLogger.Log("Starting OSX application.");

            var appFileName = FindExecutable(fileName => fileName.EndsWith(".app") && IsInsideRootDirectory(fileName));

            if (appFileName == null)
            {
                throw new InvalidOperationException("Couldn't find executable bundle for Mac OSX.");
            }

            foreach (var fileName in _app.LocalMetaData.GetRegisteredEntries())
            {
                string filePath = _app.LocalDirectory.Path.PathCombine(fileName);

                if (MagicBytes.IsMacExecutable(filePath))
                {
                    Chmod(filePath, "+x");
                }
            }

            string appFilePath = _app.LocalDirectory.Path.PathCombine(appFileName);
            string appDirPath = Path.GetDirectoryName(appFilePath) ?? string.Empty;

            DebugLogger.Log(string.Format("Found executable {0}", appFilePath));

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "open",
                Arguments = string.Format("\"{0}\"", _app.LocalDirectory.Path.PathCombine(appFileName)),
                WorkingDirectory = appDirPath
            };

            Process.Start(processStartInfo);
        }

        private void StartLinuxApplication()
        {
            DebugLogger.Log("Starting Linux application.");

            var appFileName = FindExecutable(fileName => IsInsideRootDirectory(fileName) && 
                MagicBytes.IsLinuxExecutable(_app.LocalDirectory.Path.PathCombine(fileName)));

            if (appFileName == null)
            {
                throw new InvalidOperationException("Couldn\'t find executable file for Linux.");
            }

            string appFilePath = _app.LocalDirectory.Path.PathCombine(appFileName);
            string appDirPath = Path.GetDirectoryName(appFilePath) ?? string.Empty;

            DebugLogger.Log(string.Format("Found executable {0}", appFilePath));

            foreach (var fileName in _app.LocalMetaData.GetRegisteredEntries())
            {
                string filePath = _app.LocalDirectory.Path.PathCombine(fileName);

                if (MagicBytes.IsLinuxExecutable(filePath))
                {
                    Chmod(filePath, "+x");
                }
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = appFilePath,
                WorkingDirectory = appDirPath
            };

            Process.Start(processStartInfo);
        }

        private void StartWindowsApplication()
        {
            DebugLogger.Log("Starting Windows application.");

            var appFileName = FindExecutable(fileName => fileName.EndsWith(".exe") && IsInsideRootDirectory(fileName));

            if (appFileName == null)
            {
                throw new InvalidOperationException("Couldn't find executable bundle for Windows.");
            }

            string appFilePath = _app.LocalDirectory.Path.PathCombine(appFileName);
            string appDirPath = Path.GetDirectoryName(appFilePath) ?? string.Empty;

            DebugLogger.Log(string.Format("Found executable {0}", appFilePath));

            var processStartInfo = new ProcessStartInfo
            {
                FileName = appFilePath,
                WorkingDirectory = appDirPath
            };

            Process.Start(processStartInfo);
        }

        private void Chmod(string filePath, string permissions)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "/bin/chmod",
                    Arguments = string.Format("{0} \"{1}\"", permissions, filePath)
                }
            };
            process.Start();
            process.WaitForExit();
        }
    }
}