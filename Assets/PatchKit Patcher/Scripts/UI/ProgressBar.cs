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

        private void SetBar(float min, float max)
        {
            var anchorMax = Image.rectTransform.anchorMax;
            var anchorMin = Image.rectTransform.anchorMin;

            anchorMin.x = Mathf.Clamp(min, 0f, 1f);
            anchorMax.x = Mathf.Clamp(max, 0f, 1f);

            Image.rectTransform.anchorMax = anchorMax;
            Image.rectTransform.anchorMin = anchorMin;
        }

        private void SetProgress(double progress)
        {
            Text.text = progress >= 0.0 ? progress.ToString("0.0%") : "";
            float visualProgress = progress >= 0.0 ? (float) progress : 0.0f;

            if (visualProgress > 0)
            {
                _isIdle = false;
            }

            SetBar(0, visualProgress);
        }

        private void Start()
        {
            Patcher.Instance.UpdaterStatus
                .SelectSwitchOrDefault(s => s.Progress, -1.0)
                .ObserveOnMainThread()
                .Subscribe(SetProgress)
                .AddTo(this);
        }


        private bool _isIdle = true;
        private const float IdleBarWidth = 0.2f;
        private const float IdleBarSpeed = 1.2f;
        private float _idleProgress = -IdleBarWidth;
        private void Update()
        {
            if (_isIdle)
            {
                SetBar(_idleProgress, _idleProgress + IdleBarWidth);

                _idleProgress += Time.deltaTime * IdleBarSpeed;

                if (_idleProgress >= 1)
                {
                    _idleProgress = -IdleBarWidth;
                }
            }
        }
    }
}