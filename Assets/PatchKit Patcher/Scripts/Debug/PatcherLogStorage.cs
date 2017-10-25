using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Text;
using Ionic.Zlib;
using Newtonsoft.Json;
using UnityEngine;

#if UNITY_5_6_OR_NEWER
using UnityEngine.Networking;
#else
using UnityEngine.Experimental.Networking;
#endif

namespace PatchKit.Unity.Patcher.Debug
{
    public class PatcherLogStorage
    {
        private const string PutUrlRequestUrl = "https://se5ia30ji3.execute-api.us-west-2.amazonaws.com/production/v1/request-put-url";

        private static readonly DebugLogger DebugLogger = new DebugLogger(typeof(PatcherLogStorage));

        public Guid Guid { get; private set; }

        public bool IsLogBeingSent { get; private set; }

        private bool _shouldAbortSending;

        public PatcherLogStorage()
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

            DebugLogger.Log("Sending log...");

            DebugLogger.Log("Requesting PUT URL...");

            var putLinkRequest = new PutLinkRequest()
            {
                AppId = "patcher-unity",
                Version = PatcherInfo.GetVersion(),
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

            yield return putUrlRequest.Send();

            if (putUrlRequest.isError)
            {
                DebugLogger.LogError("Error while requesting PUT URL: " + putUrlRequest.error);
                IsLogBeingSent = false;
                yield break;
            }


            var responseText = putUrlRequest.downloadHandler.text;
            DebugLogger.Log("Got response: " + responseText);

            var requestPutUrlJson = JsonConvert.DeserializeObject<PutLinkResponse>(responseText);
            var putUrl = requestPutUrlJson.Url;


            UnityWebRequest putRequest = UnityWebRequest.Put(putUrl, GetCompressedLogFileData(logFilePath));
            yield return putRequest.Send();

            if (putRequest.isError)
            {
                DebugLogger.LogError("Error while sending log file: " + putRequest.error);
                IsLogBeingSent = false;
                yield break;
            }

            DebugLogger.Log("Log file sent!");

            const float sendDelaySeconds = 5f;
            
            DebugLogger.Log(string.Format("Waiting {0} seconds before next log could be sent...", sendDelaySeconds));

            float startWaitTime = Time.unscaledTime;
            while (Time.unscaledTime - startWaitTime < sendDelaySeconds && !_shouldAbortSending)
            {
                yield return null;
            }

            _shouldAbortSending = false;

            DebugLogger.Log("Next log can be now send.");

            IsLogBeingSent = false;
        }

        private byte[] GetCompressedLogFileData(string logFilePath)
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