using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Debugging
{
    public class PatcherLogManager : MonoBehaviour
    {
        public static PatcherLogManager Instance { get; private set; }

        private StreamWriter _logWriter;
        private readonly List<string> _logBuffer = new List<string>();

        private string _logPath;
        private string _logGuid;
        private string _logUrl;

        private object _writeToLogLock = new object();

        private void CreateLogFile()
        {
            try
            {
                _logPath = Path.GetTempFileName();

                if (File.Exists(_logPath))
                {
                    File.Delete(_logPath);
                }

                _logWriter = new StreamWriter(
                    path: _logPath,
                    append: false);
            }
            catch
            {
                // For safety ignore all exceptions
            }
        }

        private void OpenLogWriter()
        {
            lock (_writeToLogLock)
            {
                if (_logWriter != null)
                {
                    return;
                }

                try
                {
                    _logWriter = new StreamWriter(
                        path: _logPath,
                        append: true);

                    foreach (var l in _logBuffer)
                    {
                        _logWriter.WriteLine(l);
                    }

                    _logBuffer.Clear();
                }
                catch
                {
                    // For safety ignore all exceptions
                }
            }
        }

        private void CloseLogWriter()
        {
            lock (_writeToLogLock)
            {
                if (_logWriter == null)
                {
                    return;
                }

                try
                {
                    _logWriter.Dispose();
                    _logWriter = null;
                }
                catch
                {
                    // For safety ignore all exceptions
                }
            }
        }

        private void WriteToLog([NotNull] string text)
        {
            lock (_writeToLogLock)
            {
                if (_logWriter != null)
                {
                    _logWriter.WriteLine(text);
                }
                else
                {
                    _logBuffer.Add(text);
                }
            }
        }

        private void Awake()
        {
            Instance = this;

            CreateLogFile();

            _logGuid = Guid.NewGuid().ToString();

            Application.logMessageReceivedThreaded += (condition, trace, type) =>
            {
                WriteToLog($"[{type}] {condition}\n{trace}");
            };
        }

        [NotNull]
        public string GetLogUrl()
        {
            return _logUrl ?? LibPatchKitApps.GetLogUrlPrediction(
                       guid: _logGuid,
                       appId: "patcher-unity");
        }

        public void SendLog()
        {
            CloseLogWriter();

            lock (_writeToLogLock)
            {
                _logUrl = LibPatchKitApps.SendLog(
                    path: _logPath,
                    guid: _logGuid,
                    appId: "patcher-unity",
                    appVersion: Version.Text);
            }

            OpenLogWriter();
        }
    }
}