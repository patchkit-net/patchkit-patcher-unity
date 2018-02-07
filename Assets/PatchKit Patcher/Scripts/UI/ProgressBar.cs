using PatchKit.Unity.Utilities;
using UniRx;
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
            Patcher.Instance.State.ObserveOnMainThread().Subscribe(state =>
            {
                if (state != PatcherState.UpdatingApp)
                {
                    SetProgress(1.0);
                }
            }).AddTo(this);

            Patcher.Instance.UpdaterStatus.SelectSwitchOrDefault(s => s.Progress, 0.0)
                .ObserveOnMainThread()
                .Subscribe(SetProgress)
                .AddTo(this);

            SetProgress(1.0);
        }
    }
}