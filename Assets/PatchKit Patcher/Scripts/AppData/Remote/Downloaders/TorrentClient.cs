using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;
using PatchKit.Logging;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents;
using PatchKit.Unity.Patcher.AppData.Remote.Downloaders.Torrents.Protocol;
using PatchKit.Unity.Patcher.Cancellation;

namespace PatchKit.Unity.Patcher.AppData.Remote.Downloaders
{
    /// <summary>
    /// Provides an easy access for torrent client program.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public sealed class TorrentClient : ITorrentClient
    {
        private readonly ITorrentClientProcessStartInfoProvider _processStartInfoProvider;

        private readonly ILogger _logger;

        private readonly Process _process;

        private readonly StreamReader _stdOutput;

        private readonly StreamWriter _stdInput;

        private bool _disposed;

        public TorrentClient(ITorrentClientProcessStartInfoProvider processStartInfoProvider, ILogger logger)
        {
            _processStartInfoProvider = processStartInfoProvider;
            _logger = logger;
            _process = StartProcess();
            _stdOutput = CreateStdOutputStream();
            _stdInput = CreateStdInputStream();
        }

        private static string ConvertPathForTorrentClient(string path)
        {
            return path.Replace("\\", "/").Replace(" ", "\\ ");
        }

        public TorrentClientStatus GetStatus(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Getting status...");

                var result = ExecuteCommand<TorrentClientStatus>("status", cancellationToken);

                _logger.LogDebug("Getting status finished.");

                return result;
            }
            catch (Exception e)
            {
                _logger.LogError("Getting status failed.", e);
                throw;
            }
        }

        public void AddTorrent(string torrentFilePath, string downloadDirectoryPath,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug(string.Format("Adding torrent from {0}...", torrentFilePath));
                _logger.LogTrace("downloadDirectoryPath = " + downloadDirectoryPath);

                var convertedTorrentFilePath = ConvertPathForTorrentClient(torrentFilePath);
                var convertedDownloadDirectoryPath = ConvertPathForTorrentClient(downloadDirectoryPath);

                _logger.LogTrace("convertedTorrentFilePath = " + convertedTorrentFilePath);
                _logger.LogTrace("convertedDownloadDirectoryPath = " + convertedDownloadDirectoryPath);

                var command = string.Format("add-torrent {0} {1}", convertedTorrentFilePath,
                    convertedDownloadDirectoryPath);

                var result = ExecuteCommand<TorrentClientMessage>(command, cancellationToken);

                _logger.LogTrace("result.Message = " + result.Message);
                _logger.LogTrace("result.Status = " + result.Status);

                if (result.Status != "ok")
                {
                    throw new AddTorrentFailureException(
                        string.Format("Invalid add-torrent status: {0}", result.Status));
                }

                _logger.LogDebug("Adding torrent finished.");
            }
            catch (Exception e)
            {
                _logger.LogError("Adding torrent failed.", e);
                throw;
            }
        }

        private TResult ExecuteCommand<TResult>([NotNull] string command, CancellationToken cancellationToken)
        {
            if (command == null) throw new ArgumentNullException("command");

            _logger.LogDebug(string.Format("Executing command {0}", command));

            _stdInput.WriteLine(command);
            _stdInput.Flush();

            string resultStr = ReadCommandResult(cancellationToken);

            _logger.LogDebug("Command execution finished. Parsing result...");
            _logger.LogTrace("result = " + resultStr);

            var result = JsonConvert.DeserializeObject<TResult>(resultStr);

            _logger.LogDebug("Parsing finished.");

            return result;
        }

        private string ReadCommandResult(CancellationToken cancellationToken)
        {
            var str = new StringBuilder();

            while (!str.ToString().EndsWith("#=end"))
            {
                cancellationToken.ThrowIfCancellationRequested();

                str.Append((char) _stdOutput.Read());
            }

            return str.ToString().Substring(0, str.Length - 5);
        }

        private StreamReader CreateStdOutputStream()
        {
            return new StreamReader(_process.StandardOutput.BaseStream, CreateStdEncoding());
        }

        private StreamWriter CreateStdInputStream()
        {
            return new StreamWriter(_process.StandardInput.BaseStream, CreateStdEncoding());
        }

        private static Encoding CreateStdEncoding()
        {
            return new UTF8Encoding(false);
        }

        private Process StartProcess()
        {
            var processStartInfo = _processStartInfoProvider.GetProcessStartInfo();
            _logger.LogTrace("processStartInfo.FileName" + processStartInfo.FileName);
            _logger.LogTrace("processStartInfo.Arguments" + processStartInfo.Arguments);

            _logger.LogDebug("Starting torrent-client process...");
            var process = Process.Start(processStartInfo);
            _logger.LogDebug("torrent-client process started.");

            return process;
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

        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _stdOutput.Dispose();
                _stdInput.Dispose();

                try
                {
                    _logger.LogDebug("Killing torrent-client process...");

                    if (!_process.HasExited)
                    {
                        _logger.LogDebug("Sending kill request and waiting one second...");
                        _process.Kill();
                        _process.WaitForExit(1000);
                        if (_process.HasExited)
                        {
                            _logger.LogDebug("torrent-client process killed.");
                        }
                        else
                        {
                            _logger.LogWarning(
                                "torrent-client process hasn't been killed. Ignoring in order to not freeze application execution.");
                        }
                    }
                    else
                    {
                        _logger.LogDebug("torrent-client process is already killed.");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("Killing torrent-client process failed.", e);
                    throw;
                }
                finally
                {
                    _process.Dispose();
                }
            }

            _disposed = true;
        }
    }
}