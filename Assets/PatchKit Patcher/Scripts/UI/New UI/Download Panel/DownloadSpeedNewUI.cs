using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    [RequireComponent(typeof(TextMeshProTranslator))]
    public class DownloadSpeedNewUI : MonoBehaviour
    {
        private TextMeshProTranslator _textMeshProTranslator;

        private string _downloadSpeedUnit;

        private void Start()
        {
            _textMeshProTranslator = GetComponent<TextMeshProTranslator>();

            var downloadStatus = Patcher.Instance.UpdaterStatus
                .SelectSwitchOrNull(u => u.LatestActiveOperation)
                .Select(s => s as IReadOnlyDownloadStatus);

            var downloadSpeedUnit = Patcher.Instance.AppInfo.Select(a => a.PatcherDownloadSpeedUnit);

            var text = downloadStatus.SelectSwitchOrDefault(status =>
            {
                var bytesPerSecond = status.BytesPerSecond;
                
                return bytesPerSecond.CombineLatest<double, string, string>(downloadSpeedUnit,
                        GetFormattedDownloadSpeed);
            }, string.Empty);

            text.ObserveOnMainThread().Subscribe(textTranslation => _textMeshProTranslator.SetText(textTranslation))
                .AddTo(this);
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
            return FormatDownloadSpeed(bytesPerSecond / Units.MB) + PatcherLanguages.OpenTag + "megabytes_sec" +
                   PatcherLanguages.CloseTag;
        }

        private static string FormatDownloadSpeedKilobytes(double bytesPerSecond)
        {
            return FormatDownloadSpeed(bytesPerSecond / Units.KB) + PatcherLanguages.OpenTag + "kilobytes_sec" +
                   PatcherLanguages.CloseTag;
        }

        private static string FormatDownloadSpeed(double s)
        {
            return s.ToString("#,#0.0");
        }
        
        private static string GetPlural(double value)
        {
            return value.ToString("0") == "1" ? string.Empty : "s";
        }
    }
}