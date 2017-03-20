using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ionic.Zlib;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.Debug
{
    public class PatcherLogSender : MonoBehaviour
    {
        private struct Log
        {
            public string Message;

            public LogType Type;

            public int GetKind()
            {
                switch (Type)
                {
                    case LogType.Error:
                    case LogType.Exception:
                    case LogType.Assert:
                        return 200;
                    default:
                        return 201;
                }
            }
        }

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(PatcherLogSender));

        private readonly object _lock = new object();

        private readonly Subject<Log> _logStream = new Subject<Log>();

        private readonly List<string> _logBuffer = new List<string>();

        private bool _hasApplicationQuit;

        private bool _shouldLogBeSent;

        private bool _isLogBeingSent;

        private int? _sendLogKind;

        private Guid _guid;

        private string _logFilePath;

        public string LogServerUrl;

        public float SendDelaySeconds;

        [ContextMenu("Test error")]
        public void TestError()
        {
            DebugLogger.LogError("Test error");
        }

        private void Awake()
        {
            _guid = Guid.NewGuid();
            _logFilePath = GetUniqueTemporaryFilePath();

            _hasApplicationQuit = false;
            _shouldLogBeSent = false;
            _isLogBeingSent = false;
            _sendLogKind = null;

            _logStream.Where(log => log.Type != LogType.Log)
                .Do(log =>
                {
                    lock (_lock)
                    {
                        _sendLogKind = _sendLogKind.HasValue ? Mathf.Min(_sendLogKind.Value, log.GetKind()) : log.GetKind();
                    }
                })
                .Throttle(TimeSpan.FromSeconds(5)).Subscribe(log =>
                {
                    lock (_lock)
                    {
                        _shouldLogBeSent = true;
                    }
                }).AddTo(this);

            _logStream.Subscribe(log =>
            {
                lock (_lock)
                {
                    _logBuffer.Add(log.Message);
                }
            }).AddTo(this);

            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        private void Update()
        {
            lock (_lock)
            {
                if (!_isLogBeingSent)
                {
                    if (_logBuffer.Count > 0)
                    {
                        WriteLogBufferToFile();
                    }

                    if (_shouldLogBeSent && _sendLogKind.HasValue)
                    {
                        _isLogBeingSent = true;
                        StartCoroutine(SendLogFile(_sendLogKind.Value));
                        _shouldLogBeSent = false;
                        _sendLogKind = null;
                    }
                }
            }
        }

        private void OnApplicationQuit()
        {
            lock (_lock)
            {
                if (_isLogBeingSent || _shouldLogBeSent)
                {
                    DebugLogger.Log("Cancelling application quit because log is being sent or is about to be sent.");
                    _hasApplicationQuit = true;
                    Application.CancelQuit();
                }
            }
        }

        private void WriteLogBufferToFile()
        {
            using (var logFileStream = new FileStream(_logFilePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (var logFileStreamWriter = new StreamWriter(logFileStream))
                {
                    foreach (var message in _logBuffer)
                    {
                        logFileStreamWriter.WriteLine(message);
                    }
                    _logBuffer.Clear();
                }
            }
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            _logStream.OnNext(new Log
            {
                Message = string.Format("{0}\n{1}", condition, stackTrace), Type = type
            });
        }

        private IEnumerator SendLogFile(int kind)
        {
            _isLogBeingSent = true;

            DebugLogger.Log("Sending log...");

            WWW www;

            try
            {
                WWWForm wwwForm = new WWWForm();
                wwwForm.AddField("kind", kind);
                wwwForm.AddField("version", PatcherInfo.GetVersion());
                wwwForm.AddField("file_guid", _guid.ToString());
                wwwForm.AddField("compression", "gzip");
                wwwForm.AddBinaryData("content", GetCompressedLogFileData());

                www = new WWW(string.Format("{0}/1/apps/{1}/logs", LogServerUrl, Patcher.Instance.Data.Value.AppSecret), wwwForm);
            }
            catch(Exception)
            {
                _isLogBeingSent = false;
                throw;
            }

            yield return www;

            var responseStatus = www.responseHeaders.ContainsKey("STATUS")
                    ? www.responseHeaders["STATUS"]
                    : "unknown";

            DebugLogger.Log(string.IsNullOrEmpty(www.error)
                ? string.Format("Log sent (response status: {0}).", responseStatus)
                : string.Format("Sending log failed: {0} (response status: {1}).\n{2}", www.error, responseStatus, www.text));

            DebugLogger.Log(string.Format("Waiting {0} seconds before next log could be sent...", SendDelaySeconds));

            float startWaitTime = Time.unscaledTime;
            while (Time.unscaledTime - startWaitTime < SendDelaySeconds && !_hasApplicationQuit)
            {
                yield return null;
            }

            DebugLogger.Log("Next log could be sent.");

            _isLogBeingSent = false;
        }

        private byte[] GetCompressedLogFileData()
        {
            using (var compressedLogFileDataStream = new MemoryStream())
            {
                using (var compressionStream = new GZipStream(compressedLogFileDataStream, CompressionMode.Compress))
                {
                    using (var logFileStream = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read))
                    {
                        const int bufferSize = 1024;
                        byte[] buffer = new byte[bufferSize];
                        int bufferRead;

                        while ((bufferRead = logFileStream.Read(buffer, 0, bufferSize)) > 0)
                        {
                            compressionStream.Write(buffer, 0, bufferRead);
                        }
                    }
                }

                return compressedLogFileDataStream.ToArray();
            }
        }

        private string GetUniqueTemporaryFilePath()
        {
            string filePath = string.Empty;

            for (int i = 0; i < 100; i ++)
            {
                filePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
                if (!File.Exists(filePath) && !Directory.Exists(filePath))
                {
                    break;
                }
            }
            return filePath;
        }

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
        }
    }
}