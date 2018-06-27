using PatchKit.Apps.Updating.AppUpdater.Status;
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

        private void SetBar(float start, float end)
        {
            var anchorMax = Image.rectTransform.anchorMax;
            var anchorMin = Image.rectTransform.anchorMin;

            anchorMin.x = Mathf.Clamp(start, 0f, 1f);
            anchorMax.x = Mathf.Clamp(end, 0f, 1f);

            Image.rectTransform.anchorMax = anchorMax;
            Image.rectTransform.anchorMin = anchorMin;
        }

        private void SetProgress(double progress)
        {
            if (progress < 0 && _isIdle)
            {
                Text.text = "Connecting...";
                return;
            }

            _isIdle = false;

            Text.text = progress > 0.0 ? progress.ToString("0.0%") : "";
            float visualProgress = progress >= 0.0 ? (float) progress : 0.0f;

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