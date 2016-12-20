using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Net
{
    internal class TorrentClient : IDisposable
    {
        private readonly string _streamingAssetsPath;

        private readonly Process _process;

        private readonly StreamReader _stdOutput;

        private readonly StreamWriter _stdInput;

        public TorrentClient(string streamingAssetsPath)
        {
            _streamingAssetsPath = streamingAssetsPath;
            _process = StartProcess();
            _stdOutput = CreateStdOutputStream();
            _stdInput = CreateStdInputStream();
        }

        /// <summary>
        /// Executes the command and returns the result.
        /// </summary>
        public JToken ExecuteCommand(string command)
        {
            WriteCommand(command);
            string resultStr = ReadCommandResult();
            return ParseCommandResult(resultStr);
        }

        private void WriteCommand(string command)
        {
            _stdInput.WriteLine(command);
            _stdInput.Flush();
        }

        private JToken ParseCommandResult(string resultStr)
        {
            return JToken.Parse(resultStr);
        }

        private string ReadCommandResult()
        {
            var str = new StringBuilder();

            while (!str.ToString().EndsWith("#=end"))
            {
                ThrowIfProcessExited();

                str.Append((char)_stdOutput.Read());
            }

            return str.ToString().Substring(0, str.Length - 5);
        }

        private void ThrowIfProcessExited()
        {
            if (_process.HasExited)
            {
                throw new Exception("torrent-client process has exited");
            }
        }

        private StreamReader CreateStdOutputStream()
        {
            return new StreamReader(_process.StandardOutput.BaseStream, CreateStdEncoding());
        }

        private StreamWriter CreateStdInputStream()
        {
            return new StreamWriter(_process.StandardInput.BaseStream, CreateStdEncoding());
        }

        private Encoding CreateStdEncoding()
        {
            return new UTF8Encoding(false);
        }

        private Process StartProcess()
        {
            var processStartInfo = GetProcessStartInfo();
            
            return Process.Start(processStartInfo);
        }

        private ProcessStartInfo GetProcessStartInfo()
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer ||
                Application.platform == RuntimePlatform.WindowsEditor)
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(_streamingAssetsPath, "torrent-client/win/torrent-client.exe"),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                return processStartInfo;
            }

            if(Application.platform == RuntimePlatform.OSXEditor ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(_streamingAssetsPath, "torrent-client/osx64/torrent-client"),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                processStartInfo.EnvironmentVariables["DYLD_LIBRARY_PATH"] = Path.Combine(_streamingAssetsPath, "torrent-client/osx64");

                return processStartInfo;
            }

            if(Application.platform == RuntimePlatform.LinuxPlayer && IntPtr.Size == 8) // Linux 64 bit
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(_streamingAssetsPath, "torrent-client/linux64/torrent-client"),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                processStartInfo.EnvironmentVariables["LD_LIBRARY_PATH"] = Path.Combine(_streamingAssetsPath, "torrent-client/linux64");

                return processStartInfo;
            }

            throw new InvalidOperationException("Unsupported platform by torrent-client.");
        }

        void IDisposable.Dispose()
        {
            _stdOutput.Dispose();
            _stdInput.Dispose();

            if (!_process.HasExited)
            {
                _process.Kill();
            }
        }
    }
}
