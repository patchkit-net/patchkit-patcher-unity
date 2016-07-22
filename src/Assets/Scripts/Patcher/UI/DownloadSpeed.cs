using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    public class DownloadSpeed : MonoBehaviour
    {
        public Text Text;

        private void Update()
        {
            var status = PatcherApplication.Instance.Patcher.Status;

            Text.text = status.IsDownloading ? status.DownloadSpeed.ToString("0.0 kB/sec.") : string.Empty;
        }
    }
}
