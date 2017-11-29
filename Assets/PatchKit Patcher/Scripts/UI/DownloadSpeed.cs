using System;
using PatchKit.Unity.Patcher.Status;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    public class DownloadSpeed : MonoBehaviour
    {
        public Text Text;

        private void Start()
        {
            Patcher.Instance.UpdateAppStatusChanged += status =>
            {
                string speed = (status.DownloadBytesPerSecond / 1024.0).ToString("0.0 kB/sec.");
                string eta = (status.DownloadBytes > 0 && status.DownloadBytes < status.DownloadTotalBytes)
                    ? " ready in " + GetRemainingTimeFormated(GetRemainingTime(status))
                    : string.Empty;

                Text.text = status.IsDownloading ? speed + eta : string.Empty;
            };

            Text.text = string.Empty;
        }

        private float GetRemainingTime(OverallStatus status)
        {
            float remainingBytes = status.DownloadTotalBytes - status.DownloadBytes;
            return remainingBytes / (float)status.DownloadBytesPerSecond;
        }

        private string GetRemainingTimeFormated(float value)
        {
            TimeSpan span = TimeSpan.FromSeconds(value);
            return span.Days > 0 ? string.Format("{0:0} day", span.Days) + GetPlural(span.Days) :
                        span.Hours > 0 ? string.Format("{0:0} hour", span.Hours) + GetPlural(span.Hours) :
                            span.Minutes > 0 ? string.Format("{0:0} minute", span.Minutes) + GetPlural(span.Minutes) :
                                span.Seconds > 0 ? string.Format("{0:0} second", span.Seconds) + GetPlural(span.Seconds) : "a moment";
        }

        private string GetPlural(int value)
        {
            return value == 1 ? string.Empty : "s";
        }
    }
}