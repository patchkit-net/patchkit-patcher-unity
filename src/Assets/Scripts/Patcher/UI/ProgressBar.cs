using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    public class ProgressBar : MonoBehaviour
    {
        public Text Text;

        public Image Image;

        private void SetProgress(float progress)
        {
            Text.text = progress.ToString("0.0%");
            var anchorMax = Image.rectTransform.anchorMax;
            anchorMax.x = progress;
            Image.rectTransform.anchorMax = anchorMax;
        }

        private void Update()
        {
            var status = PatcherApplication.Instance.Patcher.Status;

            if (status.State == PatcherState.Patching)
            {
                SetProgress(status.Progress);
            }
            else if (status.State == PatcherState.Succeed)
            {
                SetProgress(1.0f);
            }
            else
            {
                SetProgress(0.0f);
            }
        }
    }
}