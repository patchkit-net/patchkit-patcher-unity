using System.Runtime.InteropServices;
using PatchKit.Apps.Updating;
using PatchKit.Patching.Unity.Extensions;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using ILogger = PatchKit.Logging.ILogger;

namespace PatchKit.Patching.Unity.UI
{
    public class ProgressBar : MonoBehaviour
    {
        private struct Data
        {
            public double Progress;
            public string Description;
            public PatcherState State;
        }

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

        private static string StateToMessage(PatcherState state)
        {
            switch (state)
            {
                case PatcherState.LoadingPatcherConfiguration:
                    return "Loading configuration...";

                case PatcherState.LoadingPatcherData:
                    return "Loading data...";

                case PatcherState.DisplayingError:
                    return "Error!";

                case PatcherState.Connecting:
                    return "Connecting...";

                default:
                    return "";
            }
        }

        private void SetProgress(UpdateData data)
        {
            UnityEngine.Debug.Log($"Updating with {data.Progress} and {data.State}");
            _isIdle = data.State != PatcherState.UpdatingApp || data.Progress < 0;

            Text.text = StateToMessage(data.State);

            if (_isIdle)
            {
                return;
            }

            double progress = data.Progress;

            if (progress >= 0)
            {
                Text.text = progress.ToString("0.0%");
                float visualProgress = (float) progress;

                SetBar(0, visualProgress);
            }
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