using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    public class DownloadSpeed : MonoBehaviour
    {
        public Text Text;

        private void Start()
        {
            PatcherApplication.Instance.Patcher.OnDownloadProgress += progress =>
            {
                Text.text = progress.KilobytesPerSecond.ToString("0.0 kB/sec.");
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