using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    public class DownloadStatus : MonoBehaviour
    {
        public Text Text;

        private void Update()
        {
            var status = PatcherApplication.Instance.Patcher.Status;

            if (status.IsDownloading)
            {
                Text.text = string.Format("{0} MB of {1} MB", (status.DownloadBytes/1024.0/1024.0).ToString("0.0"),
                    (status.DownloadTotalBytes/1024.0/1024.0).ToString("0.0"));
            }
            else
            {
                Text.text = string.Empty;
            }
        }
    }
}
