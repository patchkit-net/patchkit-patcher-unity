using System;
using System.Collections;
using PatchKit.Unity.Patcher.Status;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    public class DownloadSpeed : MonoBehaviour
    {
        public Text Text;

        private string _downloadSpeedUnit;

        private void Start()
        {
            Patcher.Instance.UpdateAppStatusChanged += status =>
            {
                string speed = GetDownloadSpeedFormated(status);
                string eta = status.DownloadBytes > 0 && status.DownloadBytes < status.DownloadTotalBytes && status.DownloadBytesPerSecond > 0.0
                    ? " ready in " + GetRemainingTimeFormated(GetRemainingTime(status))
                    : string.Empty;

                Text.text = status.IsDownloading ? speed + eta : string.Empty;
            };

            Patcher.Instance.AppInfo.Subscribe(app => { _downloadSpeedUnit = app.PatcherDownloadSpeedUnit; })
                .AddTo(this);

            Text.text = string.Empty;
        }

        private string GetDownloadSpeedFormated(OverallStatus status)
        {
            double kbPerSecond = status.DownloadBytesPerSecond / 1024.0;

            switch (_downloadSpeedUnit)
            {
                case "kilobytes":
                    return FormatDownloadSpeedKilobytes(kbPerSecond);
                case "megabytes":
                    return FormatDownloadSpeedMegabytes(kbPerSecond);
                default: // "human_readable" and any other
                {
                    return kbPerSecond > 1024.0
                        ? FormatDownloadSpeedMegabytes(kbPerSecond)
                        : FormatDownloadSpeedKilobytes(kbPerSecond);
                }
            }
        }

        private string FormatDownloadSpeedMegabytes(double kbPerSecond)
        {
            return FormatDownloadSpeed(kbPerSecond / 1024.0) + " MB/sec.";
        }

        private string FormatDownloadSpeedKilobytes(double kbPerSecond)
        {
            return FormatDownloadSpeed(kbPerSecond) + " KB/sec.";
        }

        private string FormatDownloadSpeed(double s)
        {
            return s.ToString("#,#0.0");
        }

        private float GetRemainingTime(OverallStatus status)
        {
            float remainingBytes = status.DownloadTotalBytes - status.DownloadBytes;
            return remainingBytes / (float) status.DownloadBytesPerSecond;
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