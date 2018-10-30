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

        private struct UpdateData
        {
            public double Progress;
            public PatcherState State;
        }

        private void SetBar(float start, float end)
        {
            var anchorMax = Image.rectTransform.anchorMax;
            var anchorMin = Image.rectTransform.anchorMin;

            anchorMin.x = Mathf.Clamp(start, 0f, 1f);
            anchorMax.x = Mathf.Clamp(end, 0f, 1f);

            Image.rectTransform.anchorMax = anchorMax;
            Image.rectTransform.anchorMin = anchorMin;
        }

        private void SetProgress(UpdateData data)
        {
            _isIdle = data.State == PatcherState.Connecting;

            if (data.State == PatcherState.None || data.Progress < 0)
            {
                // do nothing
                return;
            }

            if (data.State == PatcherState.DisplayingError)
            {
                Text.text = "Error!";
                SetBar(0, 0);
                return;
            }

            if (data.State == PatcherState.Connecting)
            {
                Text.text = "Connecting...";
                return;
            }

            double progress = data.Progress;

            Text.text = progress >= 0.0 ? progress.ToString("0.0%") : "";
            float visualProgress = progress >= 0.0 ? (float) progress : 0.0f;

            SetBar(0, visualProgress);
        }

        private void Start()
        {
            var progress = Patcher.Instance.UpdaterStatus
                .SelectSwitchOrDefault(s => s.Progress, -1.0);

            Patcher.Instance.State
                .CombineLatest(progress, (state, d) => new UpdateData { Progress = d, State = state })
                .ObserveOnMainThread()
                .Subscribe(SetProgress)
                .AddTo(this);
        }

        private bool _isIdle = false;
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