using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace PatchKit.Unity.Patcher.UI
{
    public class StartAppButton : MonoBehaviour
    {
        public Button Button;

        public string StartAppCustomArgs;

        private void Start()
        {
            Button.onClick.AddListener(() =>
            {
                Patcher.Instance.StartAppCustomArgs = StartAppCustomArgs;
                Patcher.Instance.SetUserDecision(Patcher.UserDecision.StartApp);
            });

            Patcher.Instance.CanStartApp.ObserveOnMainThread().Subscribe(canStartApp =>
            {
                Button.interactable = canStartApp;
            }).AddTo(this);
        }
    }
}