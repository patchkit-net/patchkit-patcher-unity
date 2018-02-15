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
            var operationStatus = Patcher.Instance.UpdaterStatus.SelectSwitchOrNull(s => s.LatestActiveOperation);

            var statusDescription = operationStatus.SelectSwitchOrDefault(s => s.Description, string.Empty);

            Patcher.Instance.State.CombineLatest(statusDescription, (state, description) =>
            {
                switch (state)
                {
                    case PatcherState.None:
                        return string.Empty;
                    case PatcherState.LoadingPatcherData:
                        return "Loading data...";
                    case PatcherState.LoadingPatcherConfiguration:
                        return "Loading configuration...";
                    case PatcherState.WaitingForUserDecision:
                        return string.Empty;
                    case PatcherState.StartingApp:
                        return "Starting application...";
                    case PatcherState.UpdatingApp:
                        return description;
                }
                return string.Empty;
            }).ObserveOnMainThread().SubscribeToText(Text).AddTo(this);
        }
    }
}