using System;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Patcher.Status;
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

        private readonly DownloadSpeedCalculator _downloadSpeedCalculator = new DownloadSpeedCalculator();

        private void Start()
        {
            var status = Patcher.Instance.UpdaterStatus
                .SelectSwitchOrNull(u => u.LatestActiveOperation)
                .Select(s => s as IReadOnlyDownloadStatus);

            var bytes = status.WhereNotNull().Select(s => (IObservable<long>) s.Bytes).Switch();
            var totalBytes = status.WhereNotNull().Select(s => (IObservable<long>) s.TotalBytes).Switch();
            var bytesPerSecond = bytes.Select(b =>
            {
                _downloadSpeedCalculator.AddSample(b, DateTime.Now);
                return _downloadSpeedCalculator.BytesPerSecond;
            });
            var remainingTime = bytes.CombineLatest<long, long, double, double?>(totalBytes, bytesPerSecond, GetRemainingTime);

            status.Subscribe(s =>
            {
                _downloadSpeedCalculator.Restart(DateTime.Now);
            }).AddTo(this);

            var downloadSpeedUnit = Patcher.Instance.AppInfo.Select(a => a.PatcherDownloadSpeedUnit);

            var statusText = bytesPerSecond.CombineLatest(downloadSpeedUnit, remainingTime, (speed, speedUnit, eta) =>
            {
                string speedText = GetDownloadSpeedFormated(speedUnit);
                string etaText = eta.HasValue
                    ? " ready in " + GetRemainingTimeFormated(eta.Value)
                    : string.Empty;

                return speedText + etaText;
            });

            status.CombineLatest(statusText, (s, t) =>
            {
                if (s == null)
                {
                    return string.Empty;
                }

                return t;
            }).ObserveOnMainThread().SubscribeToText(Text).AddTo(this);
        }

        private string GetDownloadSpeedFormated(string downloadSpeedUnit)
        {
            double kbPerSecond = _downloadSpeedCalculator.BytesPerSecond / 1024.0;

            switch (downloadSpeedUnit)
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

        private double? GetRemainingTime(long bytes, long totalBytes, double bytesPerSecond)
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

        private string GetRemainingTimeFormated(double value)
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