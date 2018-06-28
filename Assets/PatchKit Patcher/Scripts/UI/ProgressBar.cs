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

        private void SetBar(float start, float end)
        {
            var anchorMax = Image.rectTransform.anchorMax;
            var anchorMin = Image.rectTransform.anchorMin;

            anchorMin.x = Mathf.Clamp(start, 0f, 1f);
            anchorMax.x = Mathf.Clamp(end, 0f, 1f);

            Image.rectTransform.anchorMax = anchorMax;
            Image.rectTransform.anchorMin = anchorMin;
        }

        private void SetProgress(Data data)
        {
            if (data.State == PatcherState.None
             || data.State == PatcherState.LoadingPatcherConfiguration
             || data.State == PatcherState.LoadingPatcherData)
            {
                _isIdle = true;
                Text.text = "Connecting...";
                return;
            }

            double progress = data.Progress;

            Text.text = data.Description;
            _isIdle = false;

            Text.text = progress.ToString("0.0%");
            float visualProgress = (float) progress;

            SetBar(0, visualProgress);
        }

        private void Start()
        {
            var operationStatus = Patcher.Instance.UpdaterStatus.SelectSwitchOrNull(s => s.LatestActiveOperation);
            var statusDescription = operationStatus.SelectSwitchOrDefault(s => s.Description, string.Empty);
            var statusProgress = operationStatus.SelectSwitchOrDefault(s => s.Progress, 0);

            var data = Patcher.Instance.State.CombineLatest(statusDescription, statusProgress,
                (status, desc, progress) => new Data
                {
                    State =  status,
                    Progress =  progress,
                    Description = desc,
                });

            data.ObserveOnMainThread().Subscribe(SetProgress).AddTo(this);
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