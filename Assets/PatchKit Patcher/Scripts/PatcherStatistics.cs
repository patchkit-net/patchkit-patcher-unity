using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PatchKit.Unity.Patcher.Debug;
using PatchKit.Unity.Utilities;
#if UNITY_5_4_OR_NEWER
using UnityEngine.Networking;
#else
using UnityEngine.Experimental.Networking;
#endif

namespace PatchKit.Unity.Patcher
{
    public class PatcherStatistics
    {
        public struct OptionalParams
        {
            public int? VersionId;
            public string FileName;
            public long? Size;
            public long? Time;
        }

        public static void DispatchSendEvent(string eventName, OptionalParams? parameters = null)
        {
            UnityDispatcher.InvokeCoroutine(PatcherStatistics.SendEvent(eventName, parameters));
        }

        public static void DispatchSendEvent(string eventName, string appSecret, OptionalParams? parameters = null)
        {
            UnityDispatcher.InvokeCoroutine(PatcherStatistics.SendEvent(eventName, appSecret, parameters));
        }

        public static IEnumerator SendEvent(string eventName, OptionalParams? parameters = null)
        {
            string appSecret = Patcher.Instance.Data.Value.AppSecret;
            return SendEvent(eventName, appSecret, parameters);
        }

        public static IEnumerator SendEvent(string eventName, string appSecret, OptionalParams? parameters = null)
        {
            string senderId = PatcherSenderId.Get();
            string caller = string.Format("patcher_unity:{0}.{1}.{2}", Version.Major, Version.Minor, Version.Release);
            string operatingSystemFamily;

            switch (Platform.GetPlatformType())
            {
                case PlatformType.Windows:
                    operatingSystemFamily = "windows";
                    break;
                case PlatformType.OSX:
                    operatingSystemFamily = "mac";
                    break;
                case PlatformType.Linux:
                    operatingSystemFamily = "linux";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            string operatingSystemVersion = EnvironmentInfo.GetSystemVersion();

            var json = new JObject();

            json["event_name"] = eventName;
            json["sender_id"] = senderId;
            json["caller"] = caller;
            json["app_secret"] = appSecret;
            json["operating_system_family"] = operatingSystemFamily;
            json["operating_system_version"] = operatingSystemVersion;

            if (parameters.HasValue)
            {
                var v = parameters.Value;
                if (v.VersionId.HasValue)
                {
                    json["version_id"] = v.VersionId.Value;
                }

                if (v.Time.HasValue)
                {
                    json["time"] = v.Time.Value;
                }

                if (v.Size.HasValue)
                {
                    json["size"] = v.Size.Value;
                }

                if (!string.IsNullOrEmpty(v.FileName))
                {
                    json["file_name"] = v.FileName;
                }
            }

            UnityWebRequest request = new UnityWebRequest("https://stats.patchkit.net/v1/entry", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json.ToString(Formatting.None));
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            UnityEngine.Debug.Log("Sending event:\n" + json.ToString(Formatting.Indented));

            yield return request.Send();

            if (request.isError || request.responseCode != 201)
            {
                UnityEngine.Debug.LogError("Failed to send event " + eventName + " (" + request.responseCode + "):\n" + request.error);
            }
            else
            {
                UnityEngine.Debug.Log("Event " + eventName + " has been sent!");
            }
        }
    }
}