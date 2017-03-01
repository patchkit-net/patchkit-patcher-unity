using UnityEngine;
using UnityEngine.UI;

namespace PatchKit.Unity.Patcher.UI
{
    public class Status : MonoBehaviour
    {
        public Text Text;

        private void Start()
        {
            Patcher.Instance.StateChanged += state =>
            {
                if (state == PatcherState.None)
                {
                    Text.text = string.Empty;
                }
                else if (state == PatcherState.CheckingInternetConnection)
                {
                    Text.text = "Checking internet connection...";
                }
                else if (state == PatcherState.HandlingErrorMessage)
                {
                    Text.text = string.Empty;
                }
                else if (state == PatcherState.LoadingPatcherConfiguration)
                {
                    Text.text = "Loading configuration...";
                }
                else if (state == PatcherState.StartingApp)
                {
                    Text.text = "Starting application...";
                }
                else if (state == PatcherState.UpdatingApp)
                {
                    Text.text = "Updating application...";
                }
                else if (state == PatcherState.WaitingForUserDecision)
                {
                    Text.text = string.Empty;
                }
            };
        }
    }
}