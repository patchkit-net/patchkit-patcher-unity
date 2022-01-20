﻿using System;
using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.UI.Languages;
using PatchKit.Unity.Utilities;
using UniRx;
using UnityEngine;

namespace PatchKit.Unity.Patcher.UI.NewUI
{
    public class EstimatedTime : MonoBehaviour
    {
        private ITextTranslator _textTranslator;

        private void Start()
        {
            _textTranslator = GetComponent<ITextTranslator>();
            if (_textTranslator == null)
                _textTranslator = gameObject.AddComponent<TextTranslator>();

            var downloadStatus = Patcher.Instance.UpdaterStatus
                .SelectSwitchOrNull(u => u.LatestActiveOperation)
                .Select(s => s as IReadOnlyDownloadStatus);

            var text = downloadStatus.SelectSwitchOrDefault(status =>
            {
                var bytesPerSecond = status.BytesPerSecond;

                var remainingTime =
                    status.Bytes.CombineLatest<long, long, double, double?>(status.TotalBytes, bytesPerSecond,
                        GetRemainingTime);

                return remainingTime.Select<double?, string>(GetFormattedRemainingTime);
            }, string.Empty);

            text.ObserveOnMainThread().Subscribe(textTranslation => _textTranslator.SetText(textTranslation))
                .AddTo(this);
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
                return FormatPlural("{0:0} ", PatcherLanguages.OpenTag + "day" + PatcherLanguages.CloseTag,
                    span.TotalDays);
            }

            if (span.TotalHours > 1.0)
            {
                return FormatPlural("{0:0} ", PatcherLanguages.OpenTag + "hour" + PatcherLanguages.CloseTag,
                    span.TotalHours);
            }

            if (span.TotalMinutes > 1.0)
            {
                return FormatPlural("{0:0} ", PatcherLanguages.OpenTag + "minute" + PatcherLanguages.CloseTag,
                    span.TotalMinutes);
            }

            if (span.TotalSeconds > 1.0)
            {
                return FormatPlural("{0:0} ", PatcherLanguages.OpenTag + "second" + PatcherLanguages.CloseTag,
                    span.TotalSeconds);
            }

            return PatcherLanguages.OpenTag + "a_moment" + PatcherLanguages.CloseTag;
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

        private static string FormatPlural(string format, string timeUnit, double value)
        {
            return string.Format(format, value) + timeUnit + GetPlural(value);
        }

        private static string GetPlural(double value)
        {
            return value.ToString("0") == "1" ? string.Empty : "s";
        }
    }
}