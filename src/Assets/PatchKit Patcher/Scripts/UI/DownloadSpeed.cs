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
                Text.text = status.IsDownloading ? (status.DownloadBytesPerSecond / 1024.0).ToString("0.0 kB/sec.") : string.Empty;
            };

            Text.text = string.Empty;
        }
    }
}