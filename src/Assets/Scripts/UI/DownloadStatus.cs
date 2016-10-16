using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    public class DownloadStatus : MonoBehaviour
    {
        public Text Text;

        private void Start()
        {
            PatcherApplication.Instance.Patcher.OnDownloadProgress += progress =>
            {
                Text.text = string.Format("{0} MB of {1} MB", (progress.DownloadedBytes / 1024.0 / 1024.0).ToString("0.0"),
                    (progress.TotalBytes / 1024.0 / 1024.0).ToString("0.0"));
            };

            PatcherApplication.Instance.Patcher.OnStateChanged += state =>
            {
                if (state != PatcherState.Processing)
                {
                    Text.text = string.Empty;
                }
            };
        }
    }
}