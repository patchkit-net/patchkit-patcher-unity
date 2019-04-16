using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using Utilities;

namespace UI.Legacy
{
public class DownloadSpeed : MonoBehaviour
{
    public Text Text;

    private void Awake()
    {
        Patcher.Instance.StateChanged += state =>
        {
            Assert.IsNotNull(value: state);
            Assert.IsNotNull(value: Text);

            if (state.Kind != PatcherStateKind.UpdatingApp ||
                state.UpdateAppState.IsConnecting)
            {
                Text.text = string.Empty;
                return;
            }

            var remainingTime = GetRemainingTime(
                bytes: state.UpdateAppState.InstalledBytes,
                totalBytes: state.UpdateAppState.TotalBytes,
                bytesPerSecond: state.UpdateAppState.BytesPerSecond);

            string formattedRemainingTime =
                GetFormattedRemainingTime(remainingTime: remainingTime);

            string formattedDownloadSpeed = GetFormattedDownloadSpeed(
                bytesPerSecond: state.UpdateAppState.BytesPerSecond,
                downloadSpeedUnit:
                state.AppState.Info?.PatcherDownloadSpeedUnit);

            Text.text = GetStatusText(
                formattedDownloadSpeed: formattedDownloadSpeed,
                formattedRemainingTime: formattedRemainingTime);
        };
    }

    private static string GetStatusText(
        string formattedDownloadSpeed,
        string formattedRemainingTime)
    {
        return formattedDownloadSpeed + " " + formattedRemainingTime;
    }

    private static string GetFormattedDownloadSpeed(
        double bytesPerSecond,
        string downloadSpeedUnit)
    {
        switch (downloadSpeedUnit)
        {
            case "kilobytes":
                return FormatDownloadSpeedKilobytes(
                    bytesPerSecond: bytesPerSecond);
            case "megabytes":
                return FormatDownloadSpeedMegabytes(
                    bytesPerSecond: bytesPerSecond);
            // ReSharper disable once RedundantCaseLabel
            case "human_readable":
            default:
            {
                return bytesPerSecond > Units.MB
                    ? FormatDownloadSpeedMegabytes(
                        bytesPerSecond: bytesPerSecond)
                    : FormatDownloadSpeedKilobytes(
                        bytesPerSecond: bytesPerSecond);
            }
        }
    }

    private static string FormatDownloadSpeedMegabytes(double bytesPerSecond)
    {
        return FormatDownloadSpeed(s: bytesPerSecond / Units.MB) + " MB/sec.";
    }

    private static string FormatDownloadSpeedKilobytes(double bytesPerSecond)
    {
        return FormatDownloadSpeed(s: bytesPerSecond / Units.KB) + " KB/sec.";
    }

    private static string FormatDownloadSpeed(double s)
    {
        return s.ToString(format: "#,#0.0");
    }

    private static string GetFormattedRemainingTime(TimeSpan? remainingTime)
    {
        if (!remainingTime.HasValue)
        {
            return string.Empty;
        }

        var span = remainingTime.Value;

        if (span.TotalDays > 1.0)
        {
            return FormatPlural(
                format: "{0:0} day",
                value: span.TotalDays);
        }

        if (span.TotalHours > 1.0)
        {
            return FormatPlural(
                format: "{0:0} hour",
                value: span.TotalHours);
        }

        if (span.TotalMinutes > 1.0)
        {
            return FormatPlural(
                format: "{0:0} minute",
                value: span.TotalMinutes);
        }

        if (span.TotalSeconds > 1.0)
        {
            return FormatPlural(
                format: "{0:0} second",
                value: span.TotalSeconds);
        }

        return "a moment";
    }

    private static TimeSpan? GetRemainingTime(
        long bytes,
        long totalBytes,
        double bytesPerSecond)
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

        return TimeSpan.FromSeconds(value: remainingBytes / bytesPerSecond);
    }

    private static string FormatPlural(
        [NotNull] string format,
        double value)
    {
        return string.Format(
                format: format,
                arg0: value) +
            GetPlural(value: value);
    }

    private static string GetPlural(double value)
    {
        return value.ToString(format: "0") == "1" ? string.Empty : "s";
    }
}
}