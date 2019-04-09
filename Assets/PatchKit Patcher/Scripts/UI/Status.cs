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
                    case PatcherStateKindOld.None:
                        return string.Empty;
                    case PatcherStateKindOld.LoadingPatcherData:
                        return "Loading data...";
                    case PatcherStateKindOld.LoadingPatcherConfiguration:
                        return "Loading configuration...";
                    case PatcherStateKindOld.WaitingForUserDecision:
                        return string.Empty;
                    case PatcherStateKindOld.StartingApp:
                        return "Starting application...";
                    case PatcherStateKindOld.UpdatingApp:
                        return description;
                }
                return string.Empty;
            }).ObserveOnMainThread().SubscribeToText(Text).AddTo(this);
        }
    }
}