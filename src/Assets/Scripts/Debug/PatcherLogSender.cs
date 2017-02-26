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

        private readonly object _lock = new object();

        private readonly Subject<Log> _logs = new Subject<Log>();

        private readonly List<string> _logBuffer = new List<string>();

        private Guid _guid;

        private string _logFilePath;

        private bool _shouldBeSent;

        private int _kind;

        private bool _isBeingSent;

        public string LogServerUrl;

        public float SendDelaySeconds;

        private void Awake()
        {
            _guid = Guid.NewGuid();
            _logFilePath = GetUniqueTemporaryFilePath();

            _logs.Where(log => log.Type != LogType.Log).Throttle(TimeSpan.FromSeconds(5)).Subscribe(log =>
            {
                lock (_lock)
                {
                    if (!_shouldBeSent)
                    {
                        _shouldBeSent = true;
                        _kind = log.GetKind();
                    }
                    else
                    {
                        _kind = Mathf.Min(_kind, log.GetKind());
                    }
                }
            }).AddTo(this);

            _logs.Subscribe(log =>
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
                if (!_isBeingSent)
                {
                    if (_logBuffer.Count > 0)
                    {
                        WriteLogBufferToFile();
                    }

                    if (_shouldBeSent)
                    {
                        _shouldBeSent = false;
                        _isBeingSent = true;
                        StartCoroutine(SendLogFile(_kind));
                    }
                }
            }
        }

        private void OnApplicationQuit()
        {
            if (_isBeingSent)
            {
                Application.CancelQuit();
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
            _logs.OnNext(new Log
            {
                Message = string.Format("{0}\n{1}", condition, stackTrace), Type = type
            });
        }

        private IEnumerator SendLogFile(int kind)
        {
            _isBeingSent = true;

            WWWForm wwwForm = new WWWForm();
            wwwForm.AddField("kind", kind);
            wwwForm.AddField("version", PatcherInfo.GetVersion());
            wwwForm.AddField("file_guid", _guid.ToString());
            wwwForm.AddField("compression", "gzip");
            wwwForm.AddBinaryData("content", GetCompressedLogFileData());

            var www = new WWW(string.Format("{0}/apps/{1}/logs", LogServerUrl, Patcher.Instance.Data.Value.AppSecret));

            yield return www;

            yield return new WaitForSeconds(SendDelaySeconds);

            _isBeingSent = false;
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