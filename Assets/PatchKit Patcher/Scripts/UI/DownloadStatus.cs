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
            Patcher.Instance.State.ObserveOnMainThread().Subscribe(state =>
            {
                if (state != PatcherState.UpdatingApp)
                {
                    Text.text = string.Empty;
                }
            }).AddTo(this);

            Patcher.Instance.UpdateAppStatusChanged += status =>
            {
                if(status.IsDownloading)
                {
                    Text.text = string.Format("{0} MB of {1} MB", (status.DownloadBytes / 1024.0 / 1024.0).ToString("0.0"),
                        (status.DownloadTotalBytes / 1024.0 / 1024.0).ToString("0.0"));
                }
                else
                {
                    Text.text = string.Empty;
                }
            };

            Text.text = string.Empty;
        }
    }
}