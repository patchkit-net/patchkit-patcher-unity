using System;
using PatchKit.Unity.Utilities;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    public class Status : MonoBehaviour
    {
        public Text Text;

        private void Start()
        {
            Patcher.Instance.State.ObserveOnMainThread().Subscribe(state =>
            {
                switch (state)
                {
                    case PatcherState.None:
                        Text.text = string.Empty;
                        break;
                    case PatcherState.LoadingPatcherData:
                        Text.text = "Loading data...";
                        break;
                    case PatcherState.LoadingPatcherConfiguration:
                        Text.text = "Loading configuration...";
                        break;
                    case PatcherState.WaitingForUserDecision:
                        Text.text = string.Empty;
                        break;
                    case PatcherState.StartingApp:
                        Text.text = "Starting application...";
                        break;
                    case PatcherState.UpdatingApp:
                        // Managed by event below
                        break;
                }
            }).AddTo(this);

            var latestOperation = Patcher.Instance.UpdaterStatus.SelectSwitchOrNull(s => s.LatestActiveOperation);

            var latestOperationDescription = latestOperation.SelectSwitchOrDefault(s => s.Description, string.Empty);

            latestOperation.CombineLatest(latestOperationDescription, (operation, desc) =>
                {
                    if (operation == null)
                    {
                        return string.Empty;
                    }

                    return desc;
                })
                .ObserveOnMainThread()
                .SubscribeToText(Text)
                .AddTo(this);

            Text.text = string.Empty;
        }
    }
}