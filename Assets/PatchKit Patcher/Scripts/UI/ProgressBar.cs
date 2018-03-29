using PatchKit.Patching.Unity.Extensions;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Patching.Unity.UI
{
    public class ProgressBar : MonoBehaviour
    {
        public Text Text;

        public Image Image;

        private void SetProgress(double progress)
        {

            Text.text = progress >= 0.0 ? progress.ToString("0.0%") : "";
            float visualProgress = progress >= 0.0 ? (float) progress : 0.0f;

            var anchorMax = Image.rectTransform.anchorMax;
            anchorMax.x = visualProgress;
            Image.rectTransform.anchorMax = anchorMax;
        }

        private void Start()
        {
            Patcher.Instance.UpdaterStatus
                .SelectSwitchOrDefault(s => s.Progress, -1.0)
                .ObserveOnMainThread()
                .Subscribe(SetProgress)
                .AddTo(this);
        }
    }
}