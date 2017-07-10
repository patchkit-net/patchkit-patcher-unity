using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using PatchKit.Unity.Patcher.Debug;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Provides an easy access for torrent client program.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class TorrentClient : IDisposable
    {
        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(TorrentClient));

        private readonly ITorrentClientProcessStartInfoProvider _processStartInfoProvider;

        private readonly Process _process;

        private readonly StreamReader _stdOutput;

        private readonly StreamWriter _stdInput;

        private bool _disposed;

        public TorrentClient(ITorrentClientProcessStartInfoProvider processStartInfoProvider)
        {
            DebugLogger.LogConstructor();

            _processStartInfoProvider = processStartInfoProvider;

            _process = StartProcess();
            _stdOutput = CreateStdOutputStream();
            _stdInput = CreateStdInputStream();
        }

        /// <summary>
        /// Executes the command and returns the result.
        /// </summary>
        public JToken ExecuteCommand(string command)
        {
            Checks.ArgumentNotNull(command, "command");

            DebugLogger.Log(string.Format("Executing command {0}", command));

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
                throw new TorrentClientException("torrent-client process has exited.");
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
            var processStartInfo = _processStartInfoProvider.GetProcessStartInfo();

            return Process.Start(processStartInfo);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TorrentClient()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(_disposed)
            {
                return;
            }

            DebugLogger.LogDispose();

            if(disposing)
            {
                _stdOutput.Dispose();
                _stdInput.Dispose();

                if (!_process.HasExited)
                {
                    try
                    {
                        DebugLogger.Log("Killing torrent client process...");
                        _process.Kill();
                        _process.WaitForExit(1000);
                        DebugLogger.Log("Torrent client process has been killed.");
                    }
                    catch (Exception exception)
                    {
                        DebugLogger.LogWarning("Killing torrent client process has failed: an exception has occured.");
                        DebugLogger.LogException(exception);
                    }
                }
            }

            _disposed = true;
        }
    }
}
