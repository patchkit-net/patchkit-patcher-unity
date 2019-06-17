using System;
using System.Collections;
using System.ComponentModel;
using System.Text;
using System.Threading;
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
        public enum Event
        {
            [Description("content_download_started")]
            ContentDownloadStarted,
            [Description("content_download_succeeded")]
            ContentDownloadSucceeded,
            [Description("content_download_canceled")]
            ContentDownloadCanceled,
            [Description("content_download_failed")]
            ContentDownloadFailed,

            [Description("patch_download_started")]
            PatchDownloadStarted,
            [Description("patch_download_succeeded")]
            PatchDownloadSucceeded,
            [Description("patch_download_canceled")]
            PatchDownloadCanceled,
            [Description("patch_download_failed")]
            PatchDownloadFailed,

            [Description("validation_started")]
            ValidationStarted,
            [Description("validation_succeeded")]
            ValidationSucceeded,
            [Description("validation_failed")]
            ValidationFailed,
            [Description("validation_canceled")]
            ValidationCanceled,

            [Description("file_verification_failed")]
            FileVerificationFailed,
            
            [Description("license_key_verification_started")]
            LicenseKeyVerificationStarted,
            [Description("license_key_verification_succeeded")]
            LicenseKeyVerificationSucceeded,
            [Description("license_key_verification_failed")]
            LicenseKeyVerificationFailed,

            [Description("patcher_succeeded_closed")]
            PatcherSucceededClosed,
            [Description("patcher_canceled")]
            PatcherCanceled,
            [Description("patcher_started")]
            PatcherStarted,
            [Description("patcher_failed")]
            PatcherFailed,
            [Description("patcher_succeeded_game_started")]
            PatcherSucceededGameStarted
        }

        public struct OptionalParams
        {
            public int? VersionId;
            public string FileName;
            public long? Size;
            public long? Time;
        }

        public static bool TryDispatchSendEvent(Event ev, OptionalParams? parameters = null)
        {
            try
            {
                DispatchSendEvent(ev, parameters);
                return true;
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (ThreadInterruptedException)
            {
                throw;
            }
            catch
            {
                return false;
            }
        }

        public static void DispatchSendEvent(Event ev, OptionalParams? parameters = null)
        {
            UnityDispatcher.InvokeCoroutine(PatcherStatistics.SendEvent(ev, parameters));
        }

        public static void DispatchSendEvent(Event ev, string appSecret, OptionalParams? parameters = null)
        {
            UnityDispatcher.InvokeCoroutine(PatcherStatistics.SendEvent(ev, appSecret, parameters));
        }

        public static IEnumerator SendEvent(Event ev, OptionalParams? parameters = null)
        {
            string appSecret = Patcher.Instance.Data.Value.AppSecret;
            return SendEvent(ev, appSecret, parameters);
        }

        private static string GetCustomDescription(object objEnum)
        {
            var field = objEnum.GetType().GetField(objEnum.ToString());
            var attributes = (DescriptionAttribute[]) field.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Length > 0 ? attributes[0].Description : objEnum.ToString();
        }

        private static string EventName(Event value)
        {
            return GetCustomDescription(value);
        }

        private const string EventNameKey = "event_name";
        private const string SenderIdKey = "sender_id";
        private const string CallerKey = "caller";
        private const string AppSecretKey = "app_secret";
        private const string OperatingSystemFamilyKey = "operating_system_family";
        private const string OperatingSystemVersionKey = "operating_system_version";
        private const string VersionIdKey = "version_id";
        private const string TimeKey = "time";
        private const string SizeKey = "size";
        private const string FileNameKey = "file_name";

        public static IEnumerator SendEvent(Event ev, string appSecret, OptionalParams? parameters = null)
        {
            string senderId = PatcherSenderId.Get();
            string caller = string.Format("patcher_unity:{0}.{1}.{2}.{3}", Version.Major, Version.Minor, Version.Patch, Version.Hotfix);
            string operatingSystemFamily;

            string eventName = EventName(ev);

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

            json[EventNameKey] = eventName;
            json[SenderIdKey] = senderId;
            json[CallerKey] = caller;
            json[AppSecretKey] = appSecret;
            json[OperatingSystemFamilyKey] = operatingSystemFamily;
            json[OperatingSystemVersionKey] = operatingSystemVersion;

            if (parameters.HasValue)
            {
                var v = parameters.Value;
                if (v.VersionId.HasValue)
                {
                    json[VersionIdKey] = v.VersionId.Value;
                }

                if (v.Time.HasValue)
                {
                    json[TimeKey] = v.Time.Value;
                }

                if (v.Size.HasValue)
                {
                    json[SizeKey] = v.Size.Value;
                }

                if (!string.IsNullOrEmpty(v.FileName))
                {
                    json[FileNameKey] = v.FileName;
                }
            }

            UnityWebRequest request = new UnityWebRequest("https://stats.patchkit.net/v1/entry", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json.ToString(Formatting.None));
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            UnityEngine.Debug.Log("Sending event:\n" + json.ToString(Formatting.Indented));

            yield return request.Send();

#if UNITY_2017_1_OR_NEWER
            if (request.isNetworkError || request.responseCode != 201)
#else
            if (request.isError || request.responseCode != 201)
#endif
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