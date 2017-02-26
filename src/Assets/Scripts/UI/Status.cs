using System;
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
                        Text.text = "Updating application...";
                        break;
                }
            }).AddTo(this);
        }
    }
}