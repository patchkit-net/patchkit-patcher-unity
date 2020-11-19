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
                        return PatcherLanguages.GetTranslation("loading_data");
                    case PatcherState.LoadingPatcherConfiguration:
                        return PatcherLanguages.GetTranslation("loading_configuration");
                    case PatcherState.WaitingForUserDecision:
                        return string.Empty;
                    case PatcherState.StartingApp:
                        return PatcherLanguages.GetTranslation("starting_application");
                    case PatcherState.UpdatingApp:
                        return description;
                }
                return string.Empty;
            }).ObserveOnMainThread().SubscribeToText(Text).AddTo(this);
        }
    }
}