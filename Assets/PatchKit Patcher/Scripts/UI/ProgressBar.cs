using System;
using System.Collections.Generic;
using PatchKit.Unity.Patcher.AppUpdater.Status;
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

            public bool IsAppInstalled;

            public bool IsIdle;
        }

        private void SetProgressBar(float start, float end)
        {
            var anchorMax = Image.rectTransform.anchorMax;
            var anchorMin = Image.rectTransform.anchorMin;

            anchorMin.x = Mathf.Clamp(start, 0f, 1f);
            anchorMax.x = Mathf.Clamp(end, 0f, 1f);

            Image.rectTransform.anchorMax = anchorMax;
            Image.rectTransform.anchorMin = anchorMin;
        }

        private void SetProgressBarLinear(float progress)
        {
            SetProgressBar(0, progress);
        }

        private void SetProgressBarText(string text)
        {
            Text.text = text;
        }

        private void SetProgress(UpdateData data)
        {
        }

        private void SetIdle(string text)
        {
            SetProgressBarText(text);
            _isIdle = true;
        }

        private string FormatProgressForDisplay(double progress)
        {
            return string.Format("{0:0.0}", progress * 100.0) + "%";
        }

        private void OnUpdate(UpdateData data)
        {
            _isIdle = false;

            switch (data.State)
            {
                case PatcherState.LoadingPatcherData:
                case PatcherState.LoadingPatcherConfiguration:
                case PatcherState.Connecting:
                    SetIdle("Connecting...");
                    return;

                case PatcherState.UpdatingApp:
                    if (data.IsIdle)
                    {
                        SetIdle(string.Empty);
                        return;
                    }

                    if (data.Progress <= 0)
                    {
                        SetIdle("Connecting...");
                        return;
                    }

                    SetProgressBarText(FormatProgressForDisplay(data.Progress));
                    SetProgressBarLinear((float) data.Progress);
                    break;

                case PatcherState.WaitingForUserDecision:
                    if (data.IsAppInstalled)
                    {
                        SetProgressBarText(FormatProgressForDisplay(1.0));
                        SetProgressBarLinear(1);
                    }
                    else
                    {
                        SetProgressBarText(FormatProgressForDisplay(0.0));
                        SetProgressBarLinear(0);
                    }
                    break;

                case PatcherState.DisplayingError:
                    SetProgressBarText("Error...");
                    SetProgressBarLinear(0);
                    break;

                case PatcherState.StartingApp:
                    SetProgressBarText(FormatProgressForDisplay(1.0));
                    SetProgressBarLinear(1);
                    break;

                case PatcherState.None:
                default:
                    _isIdle = false;
                    break;
            }
        }

        private void Start()
        {
            var progress = Patcher.Instance.UpdaterStatus.SelectSwitchOrDefault(p => p.Progress, -1.0);
            var isUpdatingIdle = Patcher.Instance.UpdaterStatus
                .SelectSwitchOrDefault(p => (IObservable<IReadOnlyOperationStatus>) p.LatestActiveOperation, (IReadOnlyOperationStatus) null)
                .SelectSwitchOrDefault(p => p.IsIdle, false);

            Patcher.Instance.State
                .CombineLatest(progress, Patcher.Instance.IsAppInstalled, isUpdatingIdle,
                    (state, progressValue, isAppInstalled, isUpdatingIdleValue) => new UpdateData {
                        Progress = progressValue,
                        State = state,
                        IsAppInstalled = isAppInstalled,
                        IsIdle = isUpdatingIdleValue
                        })
                .ObserveOnMainThread()
                .Subscribe(OnUpdate)
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
                SetProgressBar(_idleProgress, _idleProgress + IdleBarWidth);

                _idleProgress += Time.deltaTime * IdleBarSpeed;

                if (_idleProgress >= 1)
                {
                    _idleProgress = -IdleBarWidth;
                }
            }
        }
    }
}