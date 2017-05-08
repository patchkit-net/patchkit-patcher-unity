using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zlib;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;
using UnityEngine.Experimental.Networking;

namespace PatchKit.Unity.Patcher.Debug
{
    public class PatcherLogSender : MonoBehaviour
    {
        private const string PutUrlRequestUrl = "https://se5ia30ji3.execute-api.us-west-2.amazonaws.com/production/v1/request-put-url";

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

        public bool IgnoreEditorErrors = true;

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
            if (Application.isEditor && IgnoreEditorErrors)
            {
                DebugLogger.Log("Sending log... (not really, ignored by inspector property)");
                yield break;
            }

            _isLogBeingSent = true;

            DebugLogger.Log("Sending log...");

            DebugLogger.Log("Requesting PUT URL...");

            var putLinkRequest = new PutLinkRequest()
            {
                AppId = "patcher-unity",
                Version = PatcherInfo.GetVersion(),
                Priority = kind.ToString(),
                Guid = _guid.ToString(),
                Compression = "gz"
            };

            string json = JsonConvert.SerializeObject(putLinkRequest);

            UnityWebRequest putUrlRequest = new UnityWebRequest(PutUrlRequestUrl, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            putUrlRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            putUrlRequest.downloadHandler = new DownloadHandlerBuffer();
            putUrlRequest.SetRequestHeader("Content-Type", "application/json");

            yield return putUrlRequest.Send();

            if (putUrlRequest.isError)
            {
                DebugLogger.LogError("Error while requesting PUT URL: " + putUrlRequest.error);
                _isLogBeingSent = false;
                yield break;
            }


            var responseText = putUrlRequest.downloadHandler.text;
            DebugLogger.Log("Got response: " + responseText);

            var requestPutUrlJson = JsonConvert.DeserializeObject<PutLinkResponse>(responseText);
            var putUrl = requestPutUrlJson.Url;


            UnityWebRequest putRequest = UnityWebRequest.Put(putUrl, GetCompressedLogFileData());
            yield return putRequest.Send();

            if (putRequest.isError)
            {
                DebugLogger.LogError("Error while sending log file: " + putRequest.error);
                _isLogBeingSent = false;
                yield break;
            }

            DebugLogger.Log("Log file sent!");

            DebugLogger.Log(string.Format("Waiting {0} seconds before next log could be sent...", SendDelaySeconds));

            float startWaitTime = Time.unscaledTime;
            while (Time.unscaledTime - startWaitTime < SendDelaySeconds && !_hasApplicationQuit)
            {
                yield return null;
            }

            DebugLogger.Log("Next log can be now send.");

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

        private struct PutLinkRequest
        {
            [JsonProperty("app_id")]
            public string AppId { get; set; }
            [JsonProperty("version")]
            public string Version { get; set; }
            [JsonProperty("priority")]
            public string Priority { get; set; }
            [JsonProperty("guid")]
            public string Guid { get; set; }
            [JsonProperty("compression")]
            public string Compression { get; set; }
        }

        private struct PutLinkResponse
        {
            public string Url { get; set; }
        }
    }
}