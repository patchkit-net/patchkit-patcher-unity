using System;
using PatchKit.Unity.UI.Languages;
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
                        return PatcherLanguages.GetTraduction("status_0");
                    case PatcherState.LoadingPatcherConfiguration:
                        return PatcherLanguages.GetTraduction("status_1");
                    case PatcherState.WaitingForUserDecision:
                        return string.Empty;
                    case PatcherState.StartingApp:
                        return PatcherLanguages.GetTraduction("status_2");
                    case PatcherState.UpdatingApp:
                        return description;
                }
                return string.Empty;
            }).ObserveOnMainThread().SubscribeToText(Text).AddTo(this);
        }
    }
}