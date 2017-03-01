using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Debug
{
    public class DebugLogReporter : IDisposable
    {
        private readonly object _writeLock = new object();

        private readonly List<string> _logCache = new List<string>();

        private readonly Guid _guid;

        private readonly FileStream _logStream;
        
        private readonly StreamWriter _logStreamWriter;

        private readonly int _sendDelayMillisecond;

        private readonly Thread _sendingThread;

        private bool _isBeingSent;

        private bool _shouldBeSent;

        private bool _sendLoopActive;

        private bool _disposed;

        public DebugLogReporter(int sendDelayMillisecond)
        {
            _guid = Guid.NewGuid();
            _logStream = new FileStream(Path.Combine(Path.GetTempPath(), Path.GetTempFileName()), FileMode.Create, FileAccess.Write);
            _logStreamWriter = new StreamWriter(_logStream);
            _sendDelayMillisecond = sendDelayMillisecond;
            _sendingThread = new Thread(SendLoop);
            _sendLoopActive = true;

            _sendingThread.Start();

            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            lock (_writeLock)
            {
                if (type == LogType.Error)
                {
                    _shouldBeSent = true;
                }

                var message = string.Format("[{0}] {1} {2}", type, condition, stackTrace);

                if (_isBeingSent)
                {
                    _logCache.Add(message);
                }
                else
                {
                    foreach (var cachedMessage in _logCache)
                    {
                        _logStreamWriter.WriteLine(cachedMessage);
                    }

                    _logCache.Clear();

                    _logStreamWriter.WriteLine(message);
                }
            }
        }

        private void SendLoop()
        {
            DateTime? lastLogSentTime = null;

            while (_sendLoopActive)
            {
                if (_shouldBeSent)
                {
                    if (lastLogSentTime == null ||
                        (DateTime.Now - lastLogSentTime.Value).TotalMilliseconds > _sendDelayMillisecond)
                    {
                        _shouldBeSent = false;
                        try
                        {
                            _isBeingSent = true;
                            Send();
                            lastLogSentTime = DateTime.Now;
                        }
                        catch (Exception exception)
                        {
                            // Log exception?
                        }
                        finally
                        {
                            _isBeingSent = false;
                        }
                    }
                }
            }
        }

        private void Send()
        {
            _isBeingSent = true;
            _isBeingSent = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DebugLogReporter()
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
                _logStreamWriter.Dispose();
                _logStream.Dispose();

                _sendLoopActive = false;
                _sendingThread.Join();
            }

            Application.logMessageReceivedThreaded -= OnLogMessageReceived;

            _disposed = true;
        }
    }
}