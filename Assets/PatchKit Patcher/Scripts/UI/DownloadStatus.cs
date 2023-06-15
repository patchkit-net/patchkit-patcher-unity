using PatchKit.Unity.Patcher.AppUpdater.Status;
using PatchKit.Unity.Utilities;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace PatchKit.Unity.Patcher.UI
{
    public class DownloadStatus : MonoBehaviour
    {
        public Text Text;

        private void Start()
        {
            var downloadStatus = Patcher.Instance.UpdaterStatus
                .SelectSwitchOrNull(u => u.LatestActiveOperation)
                .Select(s => s as IReadOnlyDownloadStatus);

            var text = downloadStatus.SelectSwitchOrDefault(status =>
            {
                return status.Bytes.CombineLatest(status.TotalBytes,
                    (bytes, totalBytes) => string.Format("{0:0.0} MB of {1:0.0} MB", bytes / 1024.0 / 1024.0,
                        totalBytes / 1024.0 / 1024.0));
            }, string.Empty);

            text.ObserveOnMainThread().SubscribeToText(Text).AddTo(this);
        }
    }
}