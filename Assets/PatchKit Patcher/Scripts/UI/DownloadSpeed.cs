using System;
using System.Linq;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Utilities;
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
            var downloadStatus = Patcher.Instance.UpdaterStatus
                .SelectSwitchOrNull(u => u.LatestActiveOperation)
                .Select(s => s as IReadOnlyDownloadStatus);

            var downloadSpeedUnit = Patcher.Instance.AppInfo.Select(a => a.PatcherDownloadSpeedUnit);

            var text = downloadStatus.SelectSwitchOrDefault(status =>
            {
                var bytesPerSecond = status.BytesPerSecond;

                var remainingTime =
                    status.Bytes.CombineLatest<long, long, double, double?>(status.TotalBytes, bytesPerSecond,
                        GetRemainingTime);

                var formattedRemainingTime = remainingTime.Select<double?, string>(GetFormattedRemainingTime);

                var formattedDownloadSpeed =
                    bytesPerSecond.CombineLatest<double, string, string>(downloadSpeedUnit,
                        GetFormattedDownloadSpeed);

                return formattedDownloadSpeed.CombineLatest<string, string, string>(formattedRemainingTime,
                    GetStatusText);
            }, string.Empty);

            text.ObserveOnMainThread().SubscribeToText(Text).AddTo(this);
        }

        private static string GetStatusText(string formattedDownloadSpeed, string formattedRemainingTime)
        {
            return formattedDownloadSpeed + " " + formattedRemainingTime;
        }

        private static string GetFormattedDownloadSpeed(double bytesPerSecond, string downloadSpeedUnit)
        {
            switch (downloadSpeedUnit)
            {
                case "kilobytes":
                    return FormatDownloadSpeedKilobytes(bytesPerSecond);
                case "megabytes":
                    return FormatDownloadSpeedMegabytes(bytesPerSecond);
                default: // "human_readable" and any other
                {
                    return bytesPerSecond > Units.MB
                        ? FormatDownloadSpeedMegabytes(bytesPerSecond)
                        : FormatDownloadSpeedKilobytes(bytesPerSecond);
                }
            }
        }

        private static string FormatDownloadSpeedMegabytes(double bytesPerSecond)
        {
            return FormatDownloadSpeed(bytesPerSecond / Units.MB) + " MB/sec.";
        }

        private static string FormatDownloadSpeedKilobytes(double bytesPerSecond)
        {
            return FormatDownloadSpeed(bytesPerSecond / Units.KB) + " KB/sec.";
        }

        private static string FormatDownloadSpeed(double s)
        {
            return s.ToString("#,#0.0");
        }

        private static string GetFormattedRemainingTime(double? remainingTime)
        {
            if (!remainingTime.HasValue)
            {
                return string.Empty;
            }

            var span = TimeSpan.FromSeconds(remainingTime.Value);

            if (span.TotalDays > 1.0)
            {
                return FormatPlural("{0:0} day", span.TotalDays);
            }

            if (span.TotalHours > 1.0)
            {
                return FormatPlural("{0:0} hour", span.TotalHours);
            }

            if (span.TotalMinutes > 1.0)
            {
                return FormatPlural("{0:0} minute", span.TotalMinutes);
            }

            if (span.TotalSeconds > 1.0)
            {
                return FormatPlural("{0:0} second", span.TotalSeconds);
            }

            return "a moment";
        }

        private static double? GetRemainingTime(long bytes, long totalBytes, double bytesPerSecond)
        {
            if (bytesPerSecond <= 0.0)
            {
                return null;
            }

            double remainingBytes = totalBytes - bytes;

            if (remainingBytes <= 0)
            {
                return null;
            }

            return remainingBytes / bytesPerSecond;
        }

        private static string FormatPlural(string format, double value)
        {
            return string.Format(format, value) + GetPlural(value);
        }

        private static string GetPlural(double value)
        {
            return value.ToString("0") == "1" ? string.Empty : "s";
        }
    }
}