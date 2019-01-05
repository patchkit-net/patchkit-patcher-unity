using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using PatchKit.Apps.Updating.Debug;
using UnityEngine;
#if UNITY_5_6_OR_NEWER
using UnityEngine.Networking;
#else
using UnityEngine.Experimental.Networking;
#endif

namespace PatchKit.Patching.Unity.Debug
{
    public class LogStorage
    {
        private const string PutUrlRequestUrl = "https://se5ia30ji3.execute-api.us-west-2.amazonaws.com/production/v1/request-put-url";

        private DebugLogger _debugLogger;

        public Guid Guid { get; private set; }

        public bool IsLogBeingSent { get; private set; }

        private bool _shouldAbortSending;

        public LogStorage()
        {
            Guid = Guid.NewGuid();
            
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => true; 
        }

        public void AbortSending()
        {
            _shouldAbortSending = true;
        }

        public IEnumerator SendLogFileCoroutine(string logFilePath)
        {
            while (IsLogBeingSent)
            {
                yield return null;
            }

            IsLogBeingSent = true;

            _debugLogger.Log("Sending log...");

            _debugLogger.Log("Requesting PUT URL...");

            var putLinkRequest = new PutLinkRequest()
            {
                AppId = "patcher-unity",
                Version = Version.Value,
                Priority = "201",
                Guid = Guid.ToString(),
                Compression = "gz"
            };

            string json = JsonConvert.SerializeObject(putLinkRequest);

            UnityWebRequest putUrlRequest = new UnityWebRequest(PutUrlRequestUrl, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            putUrlRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            putUrlRequest.downloadHandler = new DownloadHandlerBuffer();
            putUrlRequest.SetRequestHeader("Content-Type", "application/json");

            yield return putUrlRequest.SendWebRequest();

#if UNITY_5_6_OR_NEWER
            if (putUrlRequest.isNetworkError)
#else
            if (putUrlRequest.isError)
#endif
            {
                _debugLogger.LogError("Error while requesting PUT URL: " + putUrlRequest.error);
                IsLogBeingSent = false;
                yield break;
            }


            var responseText = putUrlRequest.downloadHandler.text;
            _debugLogger.Log("Got response: " + responseText);

            var requestPutUrlJson = JsonConvert.DeserializeObject<PutLinkResponse>(responseText);
            var putUrl = requestPutUrlJson.Url;

            yield break;
            /*
            //UnityWebRequest putRequest = UnityWebRequest.Put(putUrl, GetCompressedLogFileData(logFilePath));
            //yield return putRequest.SendWebRequest();

#if UNITY_5_6_OR_NEWER
            if (putRequest.isNetworkError)
#else
            if (putRequest.isError)
#endif
            {
                _debugLogger.LogError("Error while sending log file: " + putRequest.error);
                IsLogBeingSent = false;
                yield break;
            }

            _debugLogger.Log("Log file sent!");

            const float sendDelaySeconds = 5f;
            
            _debugLogger.Log(string.Format("Waiting {0} seconds before next log could be sent...", sendDelaySeconds));

            float startWaitTime = Time.unscaledTime;
            while (Time.unscaledTime - startWaitTime < sendDelaySeconds && !_shouldAbortSending)
            {
                yield return null;
            }

            _shouldAbortSending = false;

            _debugLogger.Log("Next log can be now send.");

            IsLogBeingSent = false;
            */ 
        }

        /*private byte[] GetCompressedLogFileData(string logFilePath)
        {
            using (var compressedLogFileDataStream = new MemoryStream())
            {
                using (var compressionStream = new GZipStream(compressedLogFileDataStream, CompressionMode.Compress))
                {
                    using (var logFileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read))
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
        }*/

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