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
                if(status.IsDownloading)
                {
                    Text.text = status.DownloadSpeed.ToString("0.0 kB/sec.");
                }
                else
                {
                    Text.text = string.Empty;
                }
            };
        }
    }
}