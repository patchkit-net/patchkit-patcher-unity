using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace PatchKit.Unity.Patcher
{
    public class AppStarter
    {
        private readonly App _app;

        public AppStarter(App app)
        {
            _app = app;
        }

        public void Start()
        {
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
            return _app.LocalData.MetaData.GetFileNames().FirstOrDefault(predicate);
        }

        private bool IsLinuxExecutable(string fileName)
        {
            string filePath = _app.LocalData.GetFilePath(fileName);

            using (FileStream executableFileStream = File.OpenRead(filePath))
            {
                using (BinaryReader executableBinaryReader = new BinaryReader(executableFileStream))
                {
                    byte[] magicBytes = executableBinaryReader.ReadBytes(4);

                    if (magicBytes.Length == 4)
                    {
                        if (magicBytes[0] == 127 && // 7F
                            magicBytes[1] == 69 && // 45 - 'E'
                            magicBytes[2] == 76 && // 4c - 'L'
                            magicBytes[3] == 70) // 46 - 'F'
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void StartOSXApplication()
        {
            var appFileName = FindExecutable(fileName => fileName.EndsWith(".app") && IsInsideRootDirectory(fileName));

            if (appFileName == null)
            {
                throw new InvalidOperationException("Couldn't find executable bundle for Mac OSX.");
            }

            foreach (var fileName in _app.LocalData.MetaData.GetFileNames())
            {
                Chmod(fileName, "+x");
            }

            string appFilePath = _app.LocalData.GetFilePath(appFileName);
            string appDirPath = Path.GetDirectoryName(appFilePath) ?? string.Empty;

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "open",
                Arguments = string.Format("\"{0}\"", _app.LocalData.GetFilePath(appFileName)),
                WorkingDirectory = appDirPath
            };

            Process.Start(processStartInfo);
        }

        private void StartLinuxApplication()
        {
            var appFileName = FindExecutable(fileName => IsInsideRootDirectory(fileName) && IsLinuxExecutable(fileName));

            if (appFileName == null)
            {
                throw new InvalidOperationException("Couldn\'t find executable file for Linux.");
            }

            string appFilePath = _app.LocalData.GetFilePath(appFileName);
            string appDirPath = Path.GetDirectoryName(appFilePath) ?? string.Empty;

            Chmod(appFilePath, "+x");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = appFilePath,
                WorkingDirectory = appDirPath
            };

            Process.Start(processStartInfo);
        }

        private void StartWindowsApplication()
        {
            var appFileName = FindExecutable(fileName => fileName.EndsWith(".exe") && IsInsideRootDirectory(fileName));

            if (appFileName == null)
            {
                throw new InvalidOperationException("Couldn't find executable bundle for Windows.");
            }

            string appFilePath = _app.LocalData.GetFilePath(appFileName);
            string appDirPath = Path.GetDirectoryName(appFilePath) ?? string.Empty;

            var processStartInfo = new ProcessStartInfo
            {
                FileName = appFilePath,
                WorkingDirectory = appDirPath
            };

            Process.Start(processStartInfo);
        }

        private void Chmod(string fileName, string permissions)
        {
            string filePath = _app.LocalData.GetFilePath(fileName);

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