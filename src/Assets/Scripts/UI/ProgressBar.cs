using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    public class ProgressBar : MonoBehaviour
    {
        public Text Text;

        public Image Image;

        private void SetProgress(double progress)
        {
            Text.text = progress.ToString("0.0%");
            var anchorMax = Image.rectTransform.anchorMax;
            anchorMax.x = (float)progress;
            Image.rectTransform.anchorMax = anchorMax;
        }

        private void Start()
        {
            PatcherApplication.Instance.Patcher.OnProgress += SetProgress;

            PatcherApplication.Instance.Patcher.OnStateChanged += state =>
            {
                if (state == PatcherState.None)
                {
                    SetProgress(0.0);
                }
                else if (state != PatcherState.Processing)
                {
                    SetProgress(1.0);
                }
            };
        }
    }
}