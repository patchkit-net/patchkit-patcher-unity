using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PatchKit.Unity.Patcher
{
    internal class PatcherAppStarter
    {
        private readonly string _path;

        public PatcherAppStarter(string path)
        {
            _path = path;
        }

        private void StartOSXApplication()
        {
            var directoryInfo = new DirectoryInfo(_path);

            var executableApp = directoryInfo.GetDirectories("*.app", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (executableApp == null)
            {
                throw new InvalidOperationException(string.Format("Couldn't find executable bundle for Mac OSX in {0}",
                    _path));
            }

            foreach (var file in executableApp.GetFiles("*", SearchOption.AllDirectories))
            {
                Chmod(file.FullName, "+x");
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "open",
                Arguments = string.Format("\"{0}\"", executableApp.FullName),
                WorkingDirectory = _path
            };

            Process.Start(processStartInfo);
        }

        private void StartLinuxApplication()
        {
            var directoryInfo = new DirectoryInfo(_path);

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
            }).FirstOrDefault();

            if (executableFile == null)
            {
                throw new InvalidOperationException(string.Format("Couldn't find executable file for Linux in {0}",
                    _path));
            }

            Chmod(executableFile.FullName, "+x");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = executableFile.FullName,
                WorkingDirectory = _path
            };

            Process.Start(processStartInfo);
        }

        private void StartWindowsApplication()
        {
            var directoryInfo = new DirectoryInfo(_path);

            var executableFile = directoryInfo.GetFiles("*.exe", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if (executableFile == null)
            {
                throw new InvalidOperationException(string.Format("Couldn't find executable file for Windows in {0}",
                    _path));
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = executableFile.FullName,
                WorkingDirectory = _path
            };

            Process.Start(processStartInfo);
        }

        public void StartApplication()
        {
            Debug.Log("Starting application.");

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

        private static void Chmod(string file, string permissions)
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = "/bin/chmod",
                    Arguments = string.Format("{0} \"{1}\"", permissions, file)
                }
            };
            process.Start();
            process.WaitForExit();
        }
    }
}