using System;
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
                string remaining = GetRemainingTimeFormated(GetRemainingTime(status));

                string eta = status.DownloadBytesPerSecond > 0.0 ? " ETA: " + remaining : string.Empty;
                Text.text = status.IsDownloading ? speed + eta : string.Empty;
            };

            Text.text = string.Empty;
        }

        private float GetRemainingTime(Unity.Patcher.Status.OverallStatus status)
        {
            float remainingBytes = status.DownloadTotalBytes - status.DownloadBytes;
            return remainingBytes / (float)status.DownloadBytesPerSecond;
        }

        private string GetRemainingTimeFormated(float remainingTime)
        {
            string result = string.Empty;
            TimeSpan t = TimeSpan.FromSeconds(remainingTime);

            result += t.Hours > 0 ? string.Format("{0:D2}h:", t.Hours) : string.Empty;
            result += t.Minutes > 0 ? string.Format("{0:D2}m:", t.Minutes) : string.Empty;
            result += t.Seconds > 0 ? string.Format("{0:D2}s", t.Seconds) : string.Empty;

            return result;
        }
    }
}